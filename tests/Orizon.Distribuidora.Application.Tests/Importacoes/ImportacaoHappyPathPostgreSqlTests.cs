using ClosedXML.Excel;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Infrastructure.Excel;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class ImportacaoHappyPathPostgreSqlTests
{
    [Fact]
    public async Task Cancelamento_da_requisicao_finaliza_historico_em_execucao()
    {
        var connection = Environment.GetEnvironmentVariable("ORIZON_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres";
        var schema = $"importacao_cancel_{Guid.NewGuid():N}";
        await CreateSchemaAsync(connection, schema);

        var testConnection = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema }.ConnectionString;
        var setupOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(testConnection, npgsql => npgsql.EnableRetryOnFailure())
            .Options;

        try
        {
            Guid companyId;
            Guid historyId;
            await using (var setupDb = new ApplicationDbContext(setupOptions))
            {
                await setupDb.Database.MigrateAsync();
                var unique = Guid.NewGuid().ToString("N");
                var company = new Company("Empresa Cancelamento", "Empresa Cancelamento", unique[..14], $"cancelamento-{unique}");
                setupDb.Companies.Add(company);
                var history = new ImportacaoHistorico(company.Id, "cancelamento.xlsx", TipoArquivoImportacao.Excel, 1);
                history.RegistrarValidacao(0, 0, 0, 0, 0, 0, 0, 0, 0, true, null, "{}");
                setupDb.ImportacoesHistorico.Add(history);
                await setupDb.SaveChangesAsync();
                companyId = company.Id;
                historyId = history.Id;
            }

            using var cancellation = new CancellationTokenSource();
            var executionOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(testConnection, npgsql => npgsql.EnableRetryOnFailure())
                .AddInterceptors(new CancelAfterCommitInterceptor(cancellation))
                .Options;
            await using (var executionDb = new ApplicationDbContext(executionOptions))
            {
                var executor = new ExecutorImportacaoProdutosService(
                    executionDb, NullLogger<ExecutorImportacaoProdutosService>.Instance);

                await Assert.ThrowsAsync<ImportacaoExecucaoException>(() =>
                    executor.ExecutarAsync(historyId, companyId, null, cancellation.Token));
            }

            await using var verificationDb = new ApplicationDbContext(setupOptions);
            var savedStatus = await verificationDb.ImportacoesHistorico.AsNoTracking()
                .Where(item => item.Id == historyId && item.CompanyId == companyId)
                .Select(item => item.Status)
                .SingleAsync();
            Assert.Equal(StatusImportacao.Falhou, savedStatus);
        }
        finally
        {
            await DropSchemaAsync(connection, schema);
        }
    }

    [Fact]
    public async Task Duzentos_e_um_produtos_percorrem_importacao_atomica_em_blocos_e_resultado_paginado()
    {
        var connection = Environment.GetEnvironmentVariable("ORIZON_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres";
        var schema = $"importacao_happy_{Guid.NewGuid():N}";
        await CreateSchemaAsync(connection, schema);

        var testConnection = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema }.ConnectionString;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(testConnection, npgsql => npgsql.EnableRetryOnFailure())
            .Options;

        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();

        try
        {
            var unique = Guid.NewGuid().ToString("N");
            var company = new Company("Empresa Importação", "Empresa Importação", unique[..14], $"importacao-{unique}");
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            var unit = new UnitOfMeasure(company.Id, "Unidade", "UN", null, 0, false);
            db.UnitsOfMeasure.Add(unit);
            await db.SaveChangesAsync();

            using var workbookStream = CreateWorkbookWithProducts(unique[..8], 201);
            var spreadsheet = await new LeitorExcelService().LerAsync(
                new ArquivoImportacaoExcel(workbookStream, "cinco-produtos.xlsx", workbookStream.Length),
                tamanhoAmostra: 10_000);
            var sheet = Assert.IsType<AbaPlanilhaImportada>(spreadsheet.AbaAtual);

            var mapping = await new MapeadorColunasService().MapearAsync(sheet.Cabecalhos);
            Assert.Equal(4, mapping.Colunas.Count);
            Assert.True(ValidadorMapeamentoColunas.Validar(mapping.Colunas, sheet.Cabecalhos).Valido);

            var historyService = new HistoricoImportacaoService(db);
            var history = await historyService.RegistrarAsync(
                company.Id,
                new ArquivoImportacaoExcel(Stream.Null, "cinco-produtos.xlsx", workbookStream.Length));
            var validationOptions = new OpcoesValidacaoImportacao(
                PermitirImportacaoParcial: false,
                Mapeamentos: mapping.Colunas,
                AbaSelecionada: sheet.Nome);
            var context = await new ContextoValidacaoImportacaoService(db).PrepararAsync(
                history.Id,
                company.Id,
                null,
                sheet.Amostra,
                mapping,
                validationOptions);
            var validation = await new ValidadorDadosImportacaoService().ValidarAsync(context);

            Assert.Equal(201, validation.TotalLinhas);
            Assert.Equal(201, validation.QuantidadeValida);
            Assert.Equal(201, validation.QuantidadeNovos);
            Assert.True(validation.PodeImportar);
            Assert.All(validation.Linhas, line =>
            {
                Assert.Equal(TipoOperacaoImportacao.Inserir, line.Operacao);
                Assert.NotNull(line.CodigoProduto);
                Assert.NotNull(line.Descricao);
                Assert.Empty(line.Erros);
            });

            await historyService.SalvarValidacaoAsync(
                company.Id, history.Id, null, validation, validationOptions);
            db.ChangeTracker.Clear();
            var persistedHistory = await db.ImportacoesHistorico.AsNoTracking()
                .SingleAsync(item => item.CompanyId == company.Id && item.Id == history.Id);
            var persistedOptions = System.Text.Json.JsonSerializer.Deserialize<OpcoesValidacaoImportacao>(
                persistedHistory.OpcoesValidacaoJson!);
            Assert.Equal("Código", persistedOptions!.Mapeamentos!["codigo"]);
            Assert.Equal("Produtos", persistedOptions.AbaSelecionada);
            var persistedValidation = await historyService.ObterValidacaoAsync(company.Id, history.Id, null, null, 1);
            Assert.NotNull(persistedValidation);
            Assert.Equal(201, persistedValidation.Resultado.QuantidadeValida);
            Assert.All(persistedValidation.Linhas, line => Assert.True(line.PodeImportar));

            var executor = new ExecutorImportacaoProdutosService(
                db, NullLogger<ExecutorImportacaoProdutosService>.Instance);
            var executorUserId = Guid.NewGuid();
            var execution = await executor.ExecutarAsync(history.Id, company.Id, executorUserId);

            Assert.Equal(StatusImportacao.Concluida, execution.StatusFinal);
            Assert.Equal(201, execution.Inseridos);
            Assert.Equal(201, execution.TotalProcessado);
            Assert.Empty(execution.Itens);
            Assert.Equal(201, await db.Products.CountAsync(product => product.CompanyId == company.Id));
            var executedHistory = await db.ImportacoesHistorico.AsNoTracking()
                .SingleAsync(item => item.CompanyId == company.Id && item.Id == history.Id);
            var executedItems = await db.ImportacaoItens.AsNoTracking()
                .Where(item => item.CompanyId == company.Id && item.ImportacaoHistoricoId == history.Id)
                .ToListAsync();
            Assert.Equal(executorUserId, executedHistory.UsuarioExecutorId);
            Assert.Equal(executorUserId, executedHistory.UpdatedBy);
            Assert.All(executedItems, item => Assert.Equal(executorUserId, item.UpdatedBy));

            var resultPage = await executor.ObterResultadoPaginaAsync(history.Id, company.Id, null, null, 1);
            Assert.NotNull(resultPage);
            Assert.Equal(50, resultPage.Itens.Count);
            Assert.All(resultPage.Itens, item => Assert.Equal(StatusLinhaImportacao.Inserida, item.Status));
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
            await DropSchemaAsync(connection, schema);
        }
    }

    private static MemoryStream CreateWorkbookWithProducts(string prefix, int quantity)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Produtos");
        sheet.Cell(1, 1).Value = "Código";
        sheet.Cell(1, 2).Value = "Descrição";
        sheet.Cell(1, 3).Value = "Unidade";
        sheet.Cell(1, 4).Value = "Preço de venda";
        for (var index = 1; index <= quantity; index++)
        {
            sheet.Cell(index + 1, 1).Value = $"{prefix}-P{index}";
            sheet.Cell(index + 1, 2).Value = $"Produto {index}";
            sheet.Cell(index + 1, 3).Value = "UN";
            sheet.Cell(index + 1, 4).Value = 10m + index;
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static async Task CreateSchemaAsync(string connection, string schema)
    {
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync();
        await using var createSchema = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin);
        await createSchema.ExecuteNonQueryAsync();
        await using var createHistory = new NpgsqlCommand(
            $"CREATE TABLE \"{schema}\".\"__EFMigrationsHistory\" (\"MigrationId\" character varying(150) NOT NULL, \"ProductVersion\" character varying(32) NOT NULL, CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY (\"MigrationId\"))",
            admin);
        await createHistory.ExecuteNonQueryAsync();
    }

    private static async Task DropSchemaAsync(string connection, string schema)
    {
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", admin);
        await drop.ExecuteNonQueryAsync();
    }

    private sealed class CancelAfterCommitInterceptor(CancellationTokenSource cancellation) : DbTransactionInterceptor
    {
        public override Task TransactionCommittedAsync(
            DbTransaction transaction,
            TransactionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            cancellation.Cancel();
            return Task.CompletedTask;
        }
    }
}
