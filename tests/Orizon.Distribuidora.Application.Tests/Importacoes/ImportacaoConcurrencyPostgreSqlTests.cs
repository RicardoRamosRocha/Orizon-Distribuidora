using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class ImportacaoConcurrencyPostgreSqlTests
{
    [Fact]
    public async Task Transicoes_concorrentes_nao_sobrescrevem_estado_persistido()
    {
        var connection = Environment.GetEnvironmentVariable("ORIZON_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres";
        var schema = $"importacao_concurrency_{Guid.NewGuid():N}";
        await CreateSchemaAsync(connection, schema);
        var testConnection = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema }.ConnectionString;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(testConnection)
            .Options;

        try
        {
            Guid companyId;
            await using (var setup = new ApplicationDbContext(options))
            {
                await setup.Database.MigrateAsync();
                var unique = Guid.NewGuid().ToString("N");
                var company = new Company("Empresa Concorrência", "Empresa Concorrência", unique[..14], $"concorrencia-{unique}");
                setup.Companies.Add(company);
                await setup.SaveChangesAsync();
                companyId = company.Id;
            }

            await AssertConflictAsync(
                history => history.IniciarExecucao(null),
                history => history.Cancelar(),
                StatusImportacao.Importando);
            await AssertConflictAsync(
                history => history.IniciarExecucao(null),
                Revalidate,
                StatusImportacao.Importando);
            await AssertConflictAsync(
                history => history.Cancelar(),
                history => history.IniciarExecucao(null),
                StatusImportacao.Cancelada);
            await AssertConflictAsync(
                Revalidate,
                history => history.IniciarExecucao(null),
                StatusImportacao.ValidacaoComErros);

            async Task AssertConflictAsync(
                Action<ImportacaoHistorico> winner,
                Action<ImportacaoHistorico> stale,
                StatusImportacao expectedStatus)
            {
                Guid historyId;
                await using (var setup = new ApplicationDbContext(options))
                {
                    var history = Ready(companyId);
                    setup.ImportacoesHistorico.Add(history);
                    await setup.SaveChangesAsync();
                    historyId = history.Id;
                }

                await using var staleDb = new ApplicationDbContext(options);
                var staleHistory = await staleDb.ImportacoesHistorico.SingleAsync(x => x.Id == historyId);

                await using (var winnerDb = new ApplicationDbContext(options))
                {
                    var winnerHistory = await winnerDb.ImportacoesHistorico.SingleAsync(x => x.Id == historyId);
                    winner(winnerHistory);
                    await winnerDb.SaveChangesAsync();
                }

                stale(staleHistory);
                await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => staleDb.SaveChangesAsync());

                await using var verification = new ApplicationDbContext(options);
                var savedStatus = await verification.ImportacoesHistorico
                    .Where(x => x.Id == historyId)
                    .Select(x => x.Status)
                    .SingleAsync();
                Assert.Equal(expectedStatus, savedStatus);
            }
        }
        finally
        {
            await DropSchemaAsync(connection, schema);
        }
    }

    private static ImportacaoHistorico Ready(Guid companyId)
    {
        var history = new ImportacaoHistorico(companyId, "concorrencia.xlsx", TipoArquivoImportacao.Excel, 1);
        history.RegistrarValidacao(1, 1, 0, 0, 1, 0, 0, 0, 0, true, null, "{}");
        return history;
    }

    private static void Revalidate(ImportacaoHistorico history) =>
        history.RegistrarValidacao(1, 0, 1, 0, 0, 0, 0, 0, 0, false, null, "{}");

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
}
