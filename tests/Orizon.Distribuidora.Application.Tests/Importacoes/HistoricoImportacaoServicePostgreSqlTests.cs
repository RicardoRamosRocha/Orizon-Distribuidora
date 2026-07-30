using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class HistoricoImportacaoServicePostgreSqlTests
{
    [Fact]
    public async Task SalvarValidacaoAsync_executes_user_transaction_inside_retrying_strategy()
    {
        var connection = Environment.GetEnvironmentVariable("ORIZON_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres";
        var schema = $"importacao_retry_{Guid.NewGuid():N}";
        await using (var adminConnection = new NpgsqlConnection(connection))
        {
            await adminConnection.OpenAsync();
            await using var createSchema = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", adminConnection);
            await createSchema.ExecuteNonQueryAsync();
            await using var createHistory = new NpgsqlCommand(
                $"CREATE TABLE \"{schema}\".\"__EFMigrationsHistory\" (\"MigrationId\" character varying(150) NOT NULL, \"ProductVersion\" character varying(32) NOT NULL, CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY (\"MigrationId\"))",
                adminConnection);
            await createHistory.ExecuteNonQueryAsync();
        }
        var testConnection = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema }.ConnectionString;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(testConnection, npgsql => npgsql.EnableRetryOnFailure())
            .Options;
        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
        var unique = Guid.NewGuid().ToString("N");
        var company = new Company("Empresa Teste", "Empresa Teste", unique[..14], $"retry-strategy-{unique}");
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        var companyId = company.Id;
        var service = new HistoricoImportacaoService(db);
        var history = await service.RegistrarAsync(
            companyId,
            new ArquivoImportacaoExcel(Stream.Null, $"retry-strategy-{Guid.NewGuid():N}.xlsx", 0));

        var warning = new ErroValidacaoImportacao(
            2, "descricao", "Produto", "IMP_AVISO", "Aviso de teste.",
            SeveridadeValidacao.Aviso, DateTimeOffset.UtcNow);
        var error = new ErroValidacaoImportacao(
            3, "codigo", null, "IMP_ERRO", "Erro de teste.",
            SeveridadeValidacao.Erro, DateTimeOffset.UtcNow);
        var lines = new[]
        {
            new ResultadoValidacaoLinha(
                2, StatusValidacaoLinha.ComAviso, "P001", "Produto",
                new Dictionary<string, object?> { ["codigo"] = "P001", ["descricao"] = "Produto" },
                new Dictionary<string, string?> { ["codigo"] = "P001", ["descricao"] = "Produto" },
                TipoOperacaoImportacao.Inserir, null, [], [warning], [], true, false),
            new ResultadoValidacaoLinha(
                3, StatusValidacaoLinha.Invalida, null, null,
                new Dictionary<string, object?>(),
                new Dictionary<string, string?> { ["codigo"] = null },
                TipoOperacaoImportacao.Bloquear, null, [error], [], [], false, false)
        };
        var result = new ResultadoValidacaoImportacao(
            2, 1, 1, 1, 1, 0, 0, 0, 0, true, lines, DateTimeOffset.UtcNow);

        try
        {
            await service.SalvarValidacaoAsync(
                companyId, history.Id, null, result, new OpcoesValidacaoImportacao());

            db.ChangeTracker.Clear();
            var savedHistory = await db.ImportacoesHistorico.AsNoTracking()
                .SingleAsync(x => x.CompanyId == companyId && x.Id == history.Id);
            var savedItems = await db.ImportacaoItens.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.ImportacaoHistoricoId == history.Id)
                .OrderBy(x => x.NumeroLinha)
                .ToListAsync();
            var savedIssues = await db.ImportacaoErros.AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.ImportacaoHistoricoId == history.Id)
                .ToListAsync();

            Assert.Equal(StatusImportacao.ProntaParaImportar, savedHistory.Status);
            Assert.Equal(2, savedHistory.TotalLinhas);
            Assert.Equal(2, savedItems.Count);
            Assert.Contains(savedItems, x => x.Status == StatusLinhaImportacao.Valida);
            Assert.Contains(savedItems, x => x.Status == StatusLinhaImportacao.ComErro);
            Assert.Equal(2, savedIssues.Count);
            Assert.Contains(savedIssues, x => x.Severidade == SeveridadeValidacao.Aviso);
            Assert.Contains(savedIssues, x => x.Severidade == SeveridadeValidacao.Erro);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
            await using var adminConnection = new NpgsqlConnection(connection);
            await adminConnection.OpenAsync();
            await using var dropSchema = new NpgsqlCommand($"DROP SCHEMA \"{schema}\" CASCADE", adminConnection);
            await dropSchema.ExecuteNonQueryAsync();
        }
    }
}
