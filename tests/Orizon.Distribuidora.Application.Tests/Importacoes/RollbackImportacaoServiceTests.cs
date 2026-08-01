using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Orizon.Distribuidora.Application.Importacoes;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;
using Orizon.Distribuidora.Infrastructure.Identity;
using Orizon.Distribuidora.Infrastructure.Services;

namespace Orizon.Distribuidora.Application.Tests.Importacoes;

public sealed class RollbackImportacaoServiceTests
{
    [Fact]
    public void ReverseInitialStock_estorna_saldo_e_preserva_auditoria()
    {
        var companyId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var item = new ImportacaoItem(companyId, importId, 2, "{}");
        var balance = new StockBalance(companyId, productId, warehouseId);
        var initialMovement = balance.Apply(
            StockMovementType.InitialBalance, 10m, "Estoque inicial", locationId,
            operationKey: $"importacao:{importId:N}:{item.Id:N}:estoque-inicial");
        balance.Apply(StockMovementType.PositiveAdjustment, 3m, "Ajuste posterior");

        var reverse = typeof(RollbackImportacaoService).GetMethod("ReverseInitialStock", BindingFlags.NonPublic | BindingFlags.Static)!;
        var inverseMovement = Assert.IsType<StockMovement>(reverse.Invoke(null,
            [importId, item, initialMovement, balance, new HashSet<string>(StringComparer.Ordinal), userId]));

        Assert.Equal(3m, balance.QuantityOnHand);
        Assert.Equal(StockMovementType.NegativeAdjustment, inverseMovement.Type);
        Assert.Equal(StockMovementDirection.Outbound, inverseMovement.Direction);
        Assert.Equal(10m, inverseMovement.Quantity);
        Assert.Equal(13m, inverseMovement.PreviousQuantity);
        Assert.Equal(3m, inverseMovement.ResultingQuantity);
        Assert.Equal(locationId, inverseMovement.InternalLocationId);
        Assert.Equal("ImportacaoHistorico", inverseMovement.ReferenceType);
        Assert.Equal(importId.ToString(), inverseMovement.ReferenceId);
        Assert.Equal($"rollback:{importId:N}:{item.Id:N}:estoque-inicial", inverseMovement.OperationKey);
        Assert.Equal(userId, inverseMovement.CreatedBy);
        Assert.Contains(initialMovement, balance.Movements);
        Assert.Equal(3, balance.Movements.Count);
    }

    [Fact]
    public void ReverseInitialStock_nao_duplica_movimentacao_inversa_existente()
    {
        var companyId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var item = new ImportacaoItem(companyId, importId, 2, "{}");
        var balance = new StockBalance(companyId, Guid.NewGuid(), Guid.NewGuid());
        var initialMovement = balance.Apply(StockMovementType.InitialBalance, 10m, "Estoque inicial");
        var rollbackKey = $"rollback:{importId:N}:{item.Id:N}:estoque-inicial";
        var existingKeys = new HashSet<string>(StringComparer.Ordinal) { rollbackKey };

        var reverse = typeof(RollbackImportacaoService).GetMethod("ReverseInitialStock", BindingFlags.NonPublic | BindingFlags.Static)!;
        var result = reverse.Invoke(null, [importId, item, initialMovement, balance, existingKeys, null]);

        Assert.Null(result);
        Assert.Equal(10m, balance.QuantityOnHand);
        Assert.Single(balance.Movements);
    }

    [Fact]
    public void Restore_restaura_todos_os_campos_armazenados_no_snapshot()
    {
        var companyId = Guid.NewGuid();
        var originalUnit = Guid.NewGuid();
        var originalCategory = Guid.NewGuid();
        var originalSubcategory = Guid.NewGuid();
        var originalBrand = Guid.NewGuid();
        var originalGroup = Guid.NewGuid();
        var originalSupplier = Guid.NewGuid();
        var originalPartner = Guid.NewGuid();
        var originalWarehouse = Guid.NewGuid();
        var originalLocation = Guid.NewGuid();
        var product = new Product(companyId, "P-1", "Descrição original", originalUnit, ProductType.Own);

        product.Update(
            product.InternalCode, null, "0012345678901", null, "Descrição original", null,
            "Complemento original", ProductType.Own, true, false, originalCategory,
            originalSubcategory, originalBrand, originalUnit, originalGroup, originalSupplier,
            originalPartner, originalWarehouse, originalLocation, "12345678", null, 12.34m, 23.45m, null, null,
            null, 5m, "Observação original");

        var importedUnit = Guid.NewGuid();
        product.Update(
            product.InternalCode, null, "9987654321098", null, "Descrição importada", null,
            "Complemento importado", ProductType.ThirdParty, false, true, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), importedUnit, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), null, null, "87654321", null, 98.76m, 87.65m, null, null,
            null, null, "Observação importada");

        var snapshot = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["Descrição"] = Change("Descrição original", "Descrição importada"),
            ["Descrição complementar"] = Change("Complemento original", "Complemento importado"),
            ["Código de barras"] = Change("0012345678901", "9987654321098"),
            ["Preço de custo"] = Change(12.34m.ToString(), 98.76m.ToString()),
            ["Preço de venda"] = Change(23.45m.ToString(), 87.65m.ToString()),
            ["Unidade"] = Change(originalUnit.ToString(), importedUnit.ToString()),
            ["Tipo"] = Change(ProductType.Own.ToString(), ProductType.ThirdParty.ToString()),
            ["Categoria"] = Change(originalCategory.ToString(), Guid.NewGuid().ToString()),
            ["Subcategoria"] = Change(originalSubcategory.ToString(), Guid.NewGuid().ToString()),
            ["Marca"] = Change(originalBrand.ToString(), Guid.NewGuid().ToString()),
            ["Grupo"] = Change(originalGroup.ToString(), Guid.NewGuid().ToString()),
            ["Fornecedor"] = Change(originalSupplier.ToString(), Guid.NewGuid().ToString()),
            ["Parceiro"] = Change(originalPartner.ToString(), Guid.NewGuid().ToString()),
            ["Depósito padrão"] = Change(originalWarehouse.ToString(), null),
            ["Local interno padrão"] = Change(originalLocation.ToString(), null),
            ["Estoque mínimo"] = Change(5m.ToString(), null),
            ["NCM"] = Change("12345678", "87654321"),
            ["Status"] = Change(false.ToString(), true.ToString()),
            ["Controla estoque"] = Change(true.ToString(), false.ToString()),
            ["Observações"] = Change("Observação original", "Observação importada")
        });

        var restore = typeof(RollbackImportacaoService).GetMethod("Restore", BindingFlags.NonPublic | BindingFlags.Static)!;
        restore.Invoke(null, [product, snapshot]);

        Assert.Equal("Descrição original", product.Name);
        Assert.Equal("Complemento original", product.Description);
        Assert.Equal("0012345678901", product.Barcode);
        Assert.Equal(12.34m, product.CostPrice);
        Assert.Equal(23.45m, product.SalePrice);
        Assert.Equal(originalUnit, product.UnitOfMeasureId);
        Assert.Equal(ProductType.Own, product.ProductType);
        Assert.Equal(originalCategory, product.CategoryId);
        Assert.Equal(originalSubcategory, product.SubcategoryId);
        Assert.Equal(originalBrand, product.BrandId);
        Assert.Equal(originalGroup, product.ProductGroupId);
        Assert.Equal(originalSupplier, product.MainSupplierId);
        Assert.Equal(originalPartner, product.PartnerId);
        Assert.Equal(originalWarehouse, product.DefaultWarehouseId);
        Assert.Equal(originalLocation, product.DefaultWarehouseLocationId);
        Assert.Equal(5m, product.MinimumStock);
        Assert.Equal("12345678", product.Ncm);
        Assert.False(product.IsActive);
        Assert.True(product.ControlsStock);
        Assert.Equal("Observação original", product.Notes);
    }

    [Fact]
    public void Snapshot_contem_todos_os_campos_modificaveis_relacionados_ao_estoque()
    {
        var warehouseId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid(), "P-1", "Produto", Guid.NewGuid(), ProductType.Own);
        product.Update(
            product.InternalCode, null, null, null, product.Name, null, null, ProductType.Own,
            true, true, null, null, null, product.UnitOfMeasureId, null, null, null,
            warehouseId, locationId, null, null, 0m, 10m, null, null, null, 3m, null);

        var snapshotMethod = typeof(ExecutorImportacaoProdutosService)
            .GetMethod("Snapshot", BindingFlags.NonPublic | BindingFlags.Static)!;
        var snapshot = Assert.IsType<Dictionary<string, string?>>(snapshotMethod.Invoke(null, [product]));

        Assert.Equal(warehouseId.ToString(), snapshot["Depósito padrão"]);
        Assert.Equal(locationId.ToString(), snapshot["Local interno padrão"]);
        Assert.Equal(3m.ToString(), snapshot["Estoque mínimo"]);
    }

    [Fact]
    public async Task Falha_parcial_recarrega_produto_saldo_e_movimentacoes_apos_savepoint()
    {
        var connection = Environment.GetEnvironmentVariable("ORIZON_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres";
        var schema = $"rollback_savepoint_{Guid.NewGuid():N}";
        await CreateSchemaAsync(connection, schema);
        var testConnection = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema }.ConnectionString;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(testConnection).Options;

        try
        {
            await using var db = new ApplicationDbContext(options);
            await db.Database.MigrateAsync();
            var unique = Guid.NewGuid().ToString("N");
            var company = new Company("Empresa Rollback", "Empresa Rollback", unique[..14], $"rollback-{unique}");
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            var unit = new UnitOfMeasure(company.Id, "Unidade", "UN", null, 0, false);
            var warehouse = new Warehouse(company.Id, "Depósito", "DEP", null, true);
            var product = new Product(company.Id, "P-1", "Produto", unit.Id, ProductType.Own);
            db.AddRange(unit, warehouse, product);
            await db.SaveChangesAsync();
            var importId = Guid.NewGuid();
            var item = new ImportacaoItem(company.Id, importId, 2, "{}");
            var balance = new StockBalance(company.Id, product.Id, warehouse.Id);
            var importKey = $"importacao:{importId:N}:{item.Id:N}:estoque-inicial";
            var rollbackKey = $"rollback:{importId:N}:{item.Id:N}:estoque-inicial";
            var initialMovement = balance.Apply(StockMovementType.InitialBalance, 10m, "Estoque inicial", operationKey: importKey);
            db.StockBalances.Add(balance);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            product = await db.Products.SingleAsync(current => current.Id == product.Id);
            balance = await db.StockBalances.SingleAsync(current => current.Id == balance.Id);
            initialMovement = await db.StockMovements.AsNoTracking().SingleAsync(current => current.OperationKey == importKey);
            var balances = new Dictionary<Guid, StockBalance> { [balance.Id] = balance };
            var initialMovements = new Dictionary<string, StockMovement>(StringComparer.Ordinal) { [importKey] = initialMovement };
            var rollbackKeys = new HashSet<string>(StringComparer.Ordinal);
            var service = new RollbackImportacaoService(db, NullLogger<RollbackImportacaoService>.Instance);

            await using var transaction = await db.Database.BeginTransactionAsync();
            await transaction.CreateSavepointAsync("item");
            product.Deactivate();
            var inverse = balance.Apply(StockMovementType.NegativeAdjustment, 10m, "Estorno", operationKey: rollbackKey);
            rollbackKeys.Add(rollbackKey);
            db.StockMovements.Add(inverse);
            await db.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("item");

            var reload = typeof(RollbackImportacaoService).GetMethod("ReloadFailedPartialItemAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
            await Assert.IsAssignableFrom<Task>(reload.Invoke(service,
                [importId, item, product, balance, company.Id, balances, initialMovements, rollbackKeys, CancellationToken.None]));

            Assert.True(product.IsActive);
            Assert.Equal(10m, balances[balance.Id].QuantityOnHand);
            Assert.True(initialMovements.ContainsKey(importKey));
            Assert.DoesNotContain(rollbackKey, rollbackKeys);
            Assert.False(await db.StockMovements.AsNoTracking().AnyAsync(current => current.OperationKey == rollbackKey));
            await transaction.RollbackAsync();
        }
        finally
        {
            await DropSchemaAsync(connection, schema);
        }
    }

    [Fact]
    public async Task Rollback_parcial_isola_excecao_especifica_e_falha_integral_fica_auditada()
    {
        var connection = Environment.GetEnvironmentVariable("ORIZON_TEST_POSTGRES")
            ?? "Host=localhost;Port=5432;Database=orizon_distribuidora;Username=postgres;Password=postgres";
        var schema = $"rollback_h4_{Guid.NewGuid():N}";
        await CreateSchemaAsync(connection, schema);
        var testConnection = new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema }.ConnectionString;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(testConnection).Options;

        try
        {
            await using var db = new ApplicationDbContext(options);
            await db.Database.MigrateAsync();
            var unique = Guid.NewGuid().ToString("N");
            var company = new Company("Empresa H4", "Empresa H4", unique[..14], $"h4-{unique}");
            db.Companies.Add(company);
            await db.SaveChangesAsync();
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(), CompanyId = company.Id, UserName = $"h4-{unique}@teste.local",
                NormalizedUserName = $"H4-{unique}@TESTE.LOCAL", Email = $"h4-{unique}@teste.local",
                NormalizedEmail = $"H4-{unique}@TESTE.LOCAL", FullName = "Usuário H4",
                SecurityStamp = Guid.NewGuid().ToString(), IsActive = true
            };
            var unit = new UnitOfMeasure(company.Id, "Unidade", "UN", null, 0, false);
            var failedProduct = new Product(company.Id, "P-FALHA", "Produto atual", unit.Id, ProductType.Own);
            var restoredProduct = new Product(company.Id, "P-OK", "Produto alterado", unit.Id, ProductType.Own);
            db.AddRange(user, unit, failedProduct, restoredProduct);
            await db.SaveChangesAsync();

            var partialHistory = CompletedHistory(company.Id, 2, user.Id);
            var failedItem = UpdatedItem(company.Id, partialHistory.Id, 2, failedProduct.Id, null);
            var validSnapshot = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["Descrição"] = Change("Produto original", "Produto alterado")
            });
            var restoredItem = UpdatedItem(company.Id, partialHistory.Id, 3, restoredProduct.Id, validSnapshot);
            db.AddRange(partialHistory, failedItem, restoredItem);
            await db.SaveChangesAsync();

            var service = new RollbackImportacaoService(db, NullLogger<RollbackImportacaoService>.Instance);
            var partialResult = await service.ExecutarAsync(partialHistory.Id, company.Id, user.Id, partial: true);

            Assert.Equal(StatusImportacao.RevertidaParcialmente, partialResult.Status);
            Assert.Equal(1, partialResult.ProdutosRestaurados);
            Assert.Equal(1, partialResult.Falhas);
            db.ChangeTracker.Clear();
            var savedPartialHistory = await db.ImportacoesHistorico.AsNoTracking().SingleAsync(current => current.Id == partialHistory.Id);
            var savedPartialItems = await db.ImportacaoItens.AsNoTracking().Where(current => current.ImportacaoHistoricoId == partialHistory.Id).ToListAsync();
            Assert.Equal(user.Id, savedPartialHistory.UpdatedBy);
            Assert.All(savedPartialItems, current => Assert.Equal(user.Id, current.UpdatedBy));
            Assert.Equal("Produto atual", await db.Products.Where(current => current.Id == failedProduct.Id).Select(current => current.Name).SingleAsync());
            Assert.Equal("Produto original", await db.Products.Where(current => current.Id == restoredProduct.Id).Select(current => current.Name).SingleAsync());

            var integralHistory = CompletedHistory(company.Id, 1, user.Id);
            var integralItem = UpdatedItem(company.Id, integralHistory.Id, 2, failedProduct.Id, null);
            db.AddRange(integralHistory, integralItem);
            await db.SaveChangesAsync();

            await Assert.ThrowsAsync<RollbackImportacaoException>(() =>
                service.ExecutarAsync(integralHistory.Id, company.Id, user.Id, partial: false));
            db.ChangeTracker.Clear();
            var savedIntegralHistory = await db.ImportacoesHistorico.AsNoTracking().SingleAsync(current => current.Id == integralHistory.Id);
            Assert.Equal(StatusImportacao.RollbackFalhou, savedIntegralHistory.Status);
            Assert.Equal(user.Id, savedIntegralHistory.UpdatedBy);
            Assert.Equal(user.Id, savedIntegralHistory.UsuarioRollbackId);
            Assert.Equal(1, savedIntegralHistory.FalhasRollback);
        }
        finally
        {
            await DropSchemaAsync(connection, schema);
        }
    }

    private static object Change(object? value, object? novo) => new { Value = value, Novo = novo };

    private static ImportacaoHistorico CompletedHistory(Guid companyId, int updated, Guid userId)
    {
        var history = new ImportacaoHistorico(companyId, "rollback.xlsx", TipoArquivoImportacao.Excel, 1);
        history.RegistrarValidacao(updated, updated, 0, 0, 0, updated, updated, 0, 0, true, userId, "{}");
        history.IniciarExecucao(userId);
        history.FinalizarExecucao(updated, 0, updated, 0, 0, 0, 0);
        return history;
    }

    private static ImportacaoItem UpdatedItem(Guid companyId, Guid historyId, int line, Guid productId, string? snapshot)
    {
        var item = new ImportacaoItem(companyId, historyId, line, "{}");
        item.MarcarComoValida("{}");
        item.PrepararExecucao(OperacaoExecucaoImportacao.Atualizar);
        item.ConcluirExecucao(StatusLinhaImportacao.Atualizada, productId, "Produto atualizado.", snapshot);
        return item;
    }

    private static async Task CreateSchemaAsync(string connection, string schema)
    {
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync();
        await using var createSchema = new NpgsqlCommand($"CREATE SCHEMA \"{schema}\"", admin);
        await createSchema.ExecuteNonQueryAsync();
        await using var createHistory = new NpgsqlCommand(
            $"CREATE TABLE \"{schema}\".\"__EFMigrationsHistory\" (\"MigrationId\" character varying(150) NOT NULL, \"ProductVersion\" character varying(32) NOT NULL, CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY (\"MigrationId\"))", admin);
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
