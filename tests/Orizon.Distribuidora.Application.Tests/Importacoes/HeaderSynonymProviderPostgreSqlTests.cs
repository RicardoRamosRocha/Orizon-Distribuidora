using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class HeaderSynonymProviderPostgreSqlTests
{
    [Fact]
    public async Task Aprendizado_persiste_apenas_similaridade_invalida_cache_e_reconhece_no_proximo_mapeamento()
    {
        var connection = Environment.GetEnvironmentVariable("ORIZON_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres";
        var schema = $"header_learning_{Guid.NewGuid():N}";
        await CreateSchemaAsync(connection, schema);
        var testConnection = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema }.ConnectionString;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(testConnection).Options;

        try
        {
            await using var db = new ApplicationDbContext(options);
            await db.Database.MigrateAsync();
            var unique = Guid.NewGuid().ToString("N");
            var company = new Company("Empresa Aprendizado", "Empresa Aprendizado", unique[..14], $"aprendizado-{unique}");
            db.Companies.Add(company);
            await db.SaveChangesAsync();

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var provider = new HeaderSynonymProvider(db, cache);
            var engine = new SimilarityEngine();
            var learning = new HeaderLearningService(db, provider, engine);
            var userId = Guid.NewGuid();
            _ = await provider.GetAllSynonymsAsync(company.Id);

            var learnedCount = await learning.LearnAsync(company.Id, userId, new Dictionary<string, string>
            {
                ["codigo"] = "Identificador Loja",
                ["descricao"] = "Descrição",
                ["marca"] = "Fabricante"
            });

            Assert.Equal(1, learnedCount);
            var learned = await db.HeaderSynonyms.AsNoTracking().SingleAsync();
            Assert.Equal("codigo", learned.CampoDestino);
            Assert.Equal("Identificador Loja", learned.Sinonimo);
            Assert.Equal(HeaderLearningService.LearnedOrigin, learned.Origem);
            Assert.Equal(HeaderLearningService.LearnedPriority, learned.Prioridade);
            Assert.Equal(company.Id, learned.CompanyId);
            Assert.Equal(userId, learned.CreatedBy);
            Assert.True(learned.Ativo);

            var refreshed = await provider.GetSynonymsAsync("codigo", company.Id);
            Assert.Contains("Identificador Loja", refreshed);
            Assert.Equal(0, await learning.LearnAsync(company.Id, userId,
                new Dictionary<string, string> { ["codigo"] = " identificador   loja " }));

            var mapper = new MapeadorColunasService(engine, provider);
            var mapping = await mapper.MapearAsync(["Identificador Loja"], company.Id);
            Assert.Equal("Identificador Loja", mapping.Colunas["codigo"]);
            Assert.Equal(1, mapping.Confiancas!["codigo"]);
        }
        finally
        {
            await DropSchemaAsync(connection, schema);
        }
    }

    [Fact]
    public async Task Provider_prioriza_banco_complementa_padroes_e_utiliza_cache()
    {
        var connection = Environment.GetEnvironmentVariable("ORIZON_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres";
        var schema = $"header_synonym_{Guid.NewGuid():N}";
        await CreateSchemaAsync(connection, schema);
        var testConnection = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema }.ConnectionString;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(testConnection)
            .Options;

        try
        {
            await using var db = new ApplicationDbContext(options);
            await db.Database.MigrateAsync();
            var unique = Guid.NewGuid().ToString("N");
            var company = new Company("Empresa Sinônimos", "Empresa Sinônimos", unique[..14], $"sinonimos-{unique}");
            db.Companies.Add(company);
            db.HeaderSynonyms.AddRange(
                new HeaderSynonym("codigo", "Código interno ERP", 5, "Empresa", company.Id),
                new HeaderSynonym("codigo", "Código legado", 100, "Global"),
                new HeaderSynonym("codigo", "CÓDIGO", 50, "Global"),
                new HeaderSynonym("codigo", "Código inativo", 200, "Global", ativo: false));
            await db.SaveChangesAsync();

            using var cache = new MemoryCache(new MemoryCacheOptions());
            var provider = new HeaderSynonymProvider(db, cache);
            var first = await provider.GetSynonymsAsync("codigo", company.Id);

            Assert.Equal("Código interno ERP", first[0]);
            Assert.Equal("Código legado", first[1]);
            Assert.DoesNotContain("Código inativo", first);
            Assert.Single(first.Where(item => HeaderSynonymDictionary.Normalize(item) == "codigo"));
            Assert.Contains("sku", first);

            db.HeaderSynonyms.Add(new HeaderSynonym("codigo", "Incluído depois", 500, "Empresa", company.Id));
            await db.SaveChangesAsync();
            var cached = await provider.GetSynonymsAsync("codigo", company.Id);

            Assert.DoesNotContain("Incluído depois", cached);
        }
        finally
        {
            await DropSchemaAsync(connection, schema);
        }
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
