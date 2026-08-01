using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orizon.Distribuidora.Application.Recommendations;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Recommendations;

public sealed class RecommendationServicePostgreSqlTests
{
    [Fact]
    public async Task Service_persists_filters_and_updates_recommendations_without_generating_automatically()
    {
        var connection = Environment.GetEnvironmentVariable("ORIZON_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres";
        var schema = $"recommendations_{Guid.NewGuid():N}";
        await CreateSchemaAsync(connection, schema);
        var testConnection = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema }.ConnectionString;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(testConnection).Options;

        try
        {
            await using var db = new ApplicationDbContext(options);
            await db.Database.MigrateAsync();
            var company = CreateCompany("A");
            var otherCompany = CreateCompany("B");
            db.Companies.AddRange(company, otherCompany);
            await db.SaveChangesAsync();

            Assert.Empty(await db.Recommendations.AsNoTracking().ToListAsync());
            var service = new RecommendationService(db);
            var userId = Guid.NewGuid();
            var stock = await service.CreateAsync(new CreateRecommendationRequest(
                company.Id, "Stock", RecommendationType.Opportunity,
                RecommendationSeverity.High, "Repor estoque", "Produto abaixo do mínimo.",
                "product:1", "/stock/product/1", "{\"minimum\":5}", CreatedBy: userId));
            _ = await service.CreateAsync(new CreateRecommendationRequest(
                company.Id, "Sales", RecommendationType.Insight,
                RecommendationSeverity.Low, "Revisar venda", "Venda sem observação."));
            _ = await service.CreateAsync(new CreateRecommendationRequest(
                otherCompany.Id, "Stock", RecommendationType.Risk,
                RecommendationSeverity.Critical, "Outra empresa", "Não deve ser retornada."));
            _ = await service.CreateAsync(new CreateRecommendationRequest(
                company.Id, "Stock", RecommendationType.Anomaly,
                RecommendationSeverity.Medium, "Expirada", "Não deve ser retornada.",
                ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(-1)));

            var stockRecommendations = await service.GetActiveAsync(company.Id, "Stock");
            Assert.Single(stockRecommendations);
            Assert.Equal(stock.Id, stockRecommendations[0].Id);
            Assert.Equal(userId, (await db.Recommendations.SingleAsync(item => item.Id == stock.Id)).CreatedBy);

            await service.MarkAsReadAsync(company.Id, stock.Id, userId);
            Assert.NotNull((await service.GetActiveAsync(company.Id, "Stock")).Single().ReadAt);

            await service.DismissAsync(company.Id, stock.Id, userId);
            Assert.Empty(await service.GetActiveAsync(company.Id, "Stock"));
            var persisted = await db.Recommendations.AsNoTracking().SingleAsync(item => item.Id == stock.Id);
            Assert.NotNull(persisted.DismissedAt);
            Assert.Equal(userId, persisted.UpdatedBy);
        }
        finally
        {
            await DropSchemaAsync(connection, schema);
        }
    }

    [Fact]
    public async Task Service_does_not_allow_cross_company_updates()
    {
        var connection = Environment.GetEnvironmentVariable("ORIZON_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres";
        var schema = $"recommendation_tenant_{Guid.NewGuid():N}";
        await CreateSchemaAsync(connection, schema);
        var testConnection = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema }.ConnectionString;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(testConnection).Options;

        try
        {
            await using var db = new ApplicationDbContext(options);
            await db.Database.MigrateAsync();
            var company = CreateCompany("C");
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            var service = new RecommendationService(db);
            var recommendation = await service.CreateAsync(new CreateRecommendationRequest(
                company.Id, "Products", RecommendationType.Action,
                RecommendationSeverity.Medium, "Completar produto", "Cadastro incompleto."));

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.DismissAsync(Guid.NewGuid(), recommendation.Id, Guid.NewGuid()));
        }
        finally
        {
            await DropSchemaAsync(connection, schema);
        }
    }

    private static Company CreateCompany(string suffix)
    {
        var unique = Guid.NewGuid().ToString("N");
        return new Company($"Empresa {suffix}", $"Empresa {suffix}", unique[..14], $"recommendation-{unique}");
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
}
