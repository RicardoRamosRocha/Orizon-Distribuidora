using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orizon.Distribuidora.Application.Stock;
using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Infrastructure.Data;

namespace Orizon.Distribuidora.Infrastructure.Services;

public sealed class StockService(ApplicationDbContext db) : IStockService
{
    public Task<StockOperationResult> RegisterStockEntryAsync(Guid companyId, Guid? userId, RegisterStockMovementRequest request, CancellationToken cancellationToken = default) =>
        RegisterAsync(companyId, userId, request, StockMovementType.PurchaseReceipt, cancellationToken);

    public Task<StockOperationResult> RegisterStockIssueAsync(Guid companyId, Guid? userId, RegisterStockMovementRequest request, CancellationToken cancellationToken = default) =>
        RegisterAsync(companyId, userId, request, StockMovementType.SaleIssue, cancellationToken);

    public Task<StockOperationResult> RegisterPositiveAdjustmentAsync(Guid companyId, Guid? userId, RegisterStockMovementRequest request, CancellationToken cancellationToken = default) =>
        RegisterAsync(companyId, userId, request, StockMovementType.PositiveAdjustment, cancellationToken);

    public Task<StockOperationResult> RegisterNegativeAdjustmentAsync(Guid companyId, Guid? userId, RegisterStockMovementRequest request, CancellationToken cancellationToken = default) =>
        RegisterAsync(companyId, userId, request, StockMovementType.NegativeAdjustment, cancellationToken);

    public Task<StockOperationResult> RegisterInitialBalanceAsync(Guid companyId, Guid? userId, RegisterStockMovementRequest request, CancellationToken cancellationToken = default) =>
        RegisterAsync(companyId, userId, request, StockMovementType.InitialBalance, cancellationToken);

    private async Task<StockOperationResult> RegisterAsync(
        Guid companyId, Guid? userId, RegisterStockMovementRequest request,
        StockMovementType type, CancellationToken cancellationToken)
    {
        if (companyId == Guid.Empty) return StockOperationResult.Failure("company_required", "A empresa atual não foi identificada.");
        if (request.Quantity <= 0) return StockOperationResult.Failure("invalid_quantity", "A quantidade deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(request.Reason)) return StockOperationResult.Failure("reason_required", "O motivo é obrigatório.");

        try
        {
            StockOperationResult? result = null;
            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(request.OperationKey))
                {
                    var existing = await db.StockMovements.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.OperationKey == request.OperationKey, cancellationToken);
                    if (existing is not null)
                    {
                        result = StockOperationResult.Success(existing.Id, existing.ResultingQuantity);
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }
                }

                var product = await db.Products.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.ProductId && x.CompanyId == companyId, cancellationToken);
                if (product is null) { result = StockOperationResult.Failure("product_not_found", "Produto não encontrado."); await transaction.RollbackAsync(cancellationToken); return; }
                if (!product.ControlsStock) { result = StockOperationResult.Failure("stock_not_controlled", "O produto não controla estoque físico."); await transaction.RollbackAsync(cancellationToken); return; }

                var warehouseExists = await db.Warehouses.AsNoTracking()
                    .AnyAsync(x => x.Id == request.WarehouseId && x.CompanyId == companyId, cancellationToken);
                if (!warehouseExists) { result = StockOperationResult.Failure("warehouse_not_found", "Depósito não encontrado."); await transaction.RollbackAsync(cancellationToken); return; }

                if (request.InternalLocationId.HasValue)
                {
                    var locationIsValid = await db.InternalLocations.AsNoTracking().AnyAsync(
                        x => x.Id == request.InternalLocationId && x.CompanyId == companyId && x.WarehouseId == request.WarehouseId,
                        cancellationToken);
                    if (!locationIsValid) { result = StockOperationResult.Failure("location_not_found", "Localização não encontrada no depósito informado."); await transaction.RollbackAsync(cancellationToken); return; }
                }

                var balance = await db.StockBalances
                    .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.ProductId == request.ProductId && x.WarehouseId == request.WarehouseId, cancellationToken);
                if (balance is null)
                {
                    balance = new StockBalance(companyId, request.ProductId, request.WarehouseId) { CreatedBy = userId };
                    db.StockBalances.Add(balance);
                }

                if (type == StockMovementType.InitialBalance &&
                    await db.StockMovements.AnyAsync(x => x.CompanyId == companyId && x.StockBalanceId == balance.Id && x.Type == StockMovementType.InitialBalance, cancellationToken))
                {
                    result = StockOperationResult.Failure("initial_balance_exists", "O saldo inicial já foi registrado para este produto e depósito.");
                    await transaction.RollbackAsync(cancellationToken);
                    return;
                }

                StockMovement movement;
                try
                {
                    movement = balance.Apply(type, request.Quantity, request.Reason, request.InternalLocationId,
                        request.Notes, request.UnitCost, request.ReferenceType, request.ReferenceId,
                        request.DocumentNumber, request.OperationKey, userId, request.OccurredAt);
                }
                catch (ArgumentException ex) { result = StockOperationResult.Failure("business_rule", ex.Message); await transaction.RollbackAsync(cancellationToken); return; }
                catch (InvalidOperationException ex) { result = StockOperationResult.Failure("insufficient_stock", ex.Message); await transaction.RollbackAsync(cancellationToken); return; }

                balance.UpdatedBy = userId;
                db.StockMovements.Add(movement);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                result = StockOperationResult.Success(movement.Id, movement.ResultingQuantity);
            });
            return result ?? StockOperationResult.Failure("stock_error", "Não foi possível registrar a movimentação.");
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return StockOperationResult.Failure("concurrency_conflict", "O saldo foi alterado por outra operação. Atualize os dados e tente novamente.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.ChangeTracker.Clear();
            return StockOperationResult.Failure("duplicate_operation", "A operação já foi registrada ou o saldo foi criado simultaneamente. Tente novamente.");
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return StockOperationResult.Failure("persistence_error", "Não foi possível persistir a movimentação.");
        }
    }

    public async Task<StockBalanceDto?> GetStockBalanceAsync(Guid companyId, Guid productId, Guid warehouseId, CancellationToken cancellationToken = default) =>
        await ProjectBalances(BalanceQuery(companyId)
            .Where(x => x.ProductId == productId && x.WarehouseId == warehouseId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<StockBalanceDto>> ListStockBalancesAsync(Guid companyId, StockBalanceFilter filter, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 200);
        var query = BalanceQuery(companyId);
        if (filter.ProductId.HasValue) query = query.Where(x => x.ProductId == filter.ProductId);
        if (filter.WarehouseId.HasValue) query = query.Where(x => x.WarehouseId == filter.WarehouseId);
        if (filter.CategoryId.HasValue) query = query.Where(x => x.Product!.CategoryId == filter.CategoryId);
        if (filter.OnlyBelowMinimum) query = query.Where(x => x.Product!.MinimumStock.HasValue && x.QuantityOnHand < x.Product.MinimumStock);
        if (filter.OnlyActive) query = query.Where(x => x.Product!.IsActive);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Product!.InternalCode, $"%{term}%") || EF.Functions.ILike(x.Product.Name, $"%{term}%"));
        }
        query = filter.Status switch
        {
            StockLevelStatus.Normal => query.Where(x => x.QuantityOnHand != 0 && (!x.Product!.MinimumStock.HasValue || x.QuantityOnHand >= x.Product.MinimumStock)),
            StockLevelStatus.BelowMinimum => query.Where(x => x.Product!.MinimumStock.HasValue && x.QuantityOnHand < x.Product.MinimumStock && x.QuantityOnHand != 0),
            StockLevelStatus.OutOfStock => query.Where(x => x.QuantityOnHand == 0),
            StockLevelStatus.NoMinimum => query.Where(x => !x.Product!.MinimumStock.HasValue),
            _ => query
        };
        var total = await query.CountAsync(cancellationToken);
        query = (filter.SortBy.ToLowerInvariant(), filter.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)) switch
        {
            ("quantity", true) => query.OrderByDescending(x => x.QuantityOnHand).ThenBy(x => x.Product!.Name).ThenBy(x => x.Id),
            ("quantity", false) => query.OrderBy(x => x.QuantityOnHand).ThenBy(x => x.Product!.Name).ThenBy(x => x.Id),
            ("warehouse", true) => query.OrderByDescending(x => x.Warehouse!.Name).ThenBy(x => x.Product!.Name).ThenBy(x => x.Id),
            ("warehouse", false) => query.OrderBy(x => x.Warehouse!.Name).ThenBy(x => x.Product!.Name).ThenBy(x => x.Id),
            ("code", true) => query.OrderByDescending(x => x.Product!.InternalCode).ThenBy(x => x.Warehouse!.Name).ThenBy(x => x.Id),
            ("code", false) => query.OrderBy(x => x.Product!.InternalCode).ThenBy(x => x.Warehouse!.Name).ThenBy(x => x.Id),
            (_, true) => query.OrderByDescending(x => x.Product!.Name).ThenBy(x => x.Warehouse!.Name).ThenBy(x => x.Id),
            _ => query.OrderBy(x => x.Product!.Name).ThenBy(x => x.Warehouse!.Name).ThenBy(x => x.Id)
        };
        var items = await ProjectBalances(query.Skip((page - 1) * size).Take(size))
            .ToListAsync(cancellationToken);
        return new(items, page, size, total);
    }

    public async Task<PagedResult<StockMovementDto>> ListStockMovementsAsync(Guid companyId, StockMovementFilter filter, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, filter.Page);
        var size = Math.Clamp(filter.PageSize, 1, 200);
        var query = db.StockMovements.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (filter.ProductId.HasValue) query = query.Where(x => x.ProductId == filter.ProductId);
        if (filter.WarehouseId.HasValue) query = query.Where(x => x.WarehouseId == filter.WarehouseId);
        if (filter.Type.HasValue) query = query.Where(x => x.Type == filter.Type);
        if (filter.Direction.HasValue) query = query.Where(x => x.Direction == filter.Direction);
        if (filter.From.HasValue) query = query.Where(x => x.OccurredAt >= filter.From);
        if (filter.To.HasValue) query = query.Where(x => x.OccurredAt <= filter.To);
        if (!string.IsNullOrWhiteSpace(filter.DocumentOrReference))
        {
            var search = filter.DocumentOrReference.Trim();
            query = query.Where(x => x.DocumentNumber == search || x.ReferenceId == search);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(x => db.Products.Any(p => p.CompanyId == companyId && p.Id == x.ProductId &&
                (EF.Functions.ILike(p.InternalCode, $"%{term}%") || EF.Functions.ILike(p.Name, $"%{term}%"))));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(x => new StockMovementDto(x.Id, x.ProductId, x.WarehouseId, x.InternalLocationId,
                x.Type, x.Direction, x.Quantity, x.PreviousQuantity, x.ResultingQuantity, x.UnitCost,
                x.UnitCost * x.Quantity, x.Reason, x.Notes, x.ReferenceType, x.ReferenceId,
                x.DocumentNumber, x.OccurredAt, x.CreatedAt, x.CreatedBy,
                x.Product!.InternalCode, x.Product.Name, x.Warehouse!.Name,
                x.InternalLocation != null ? x.InternalLocation.Name : null, null))
            .ToListAsync(cancellationToken);
        return new(items, page, size, total);
    }

    public async Task<StockDashboardSummary> GetDashboardSummaryAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var controlled = await db.Products.AsNoTracking().CountAsync(x => x.CompanyId == companyId && x.IsActive && x.ControlsStock, cancellationToken);
        var balances = db.StockBalances.AsNoTracking().Where(x => x.CompanyId == companyId && x.Product!.IsActive && x.Product.ControlsStock);
        var aggregate = await balances.GroupBy(_ => 1).Select(g => new
        {
            Quantity = g.Sum(x => x.QuantityOnHand),
            Below = g.Count(x => x.Product!.MinimumStock.HasValue && x.QuantityOnHand < x.Product.MinimumStock),
            Empty = g.Count(x => x.QuantityOnHand == 0),
            Warehouses = g.Select(x => x.WarehouseId).Distinct().Count()
        }).SingleOrDefaultAsync(cancellationToken);
        var since = DateTimeOffset.UtcNow.AddDays(-30);
        var recent = await db.StockMovements.AsNoTracking().CountAsync(x => x.CompanyId == companyId && x.OccurredAt >= since, cancellationToken);
        return new(controlled, aggregate?.Quantity ?? 0, aggregate?.Below ?? 0, aggregate?.Empty ?? 0, aggregate?.Warehouses ?? 0, recent);
    }

    public async Task<StockWorkspaceOptions> GetWorkspaceOptionsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var products = await db.Products.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive && x.ControlsStock)
            .OrderBy(x => x.Name).Select(x => new StockOptionDto(x.Id, x.InternalCode + " · " + x.Name, null)).ToListAsync(cancellationToken);
        var warehouses = await db.Warehouses.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.Name).Select(x => new StockOptionDto(x.Id, x.Name, null)).ToListAsync(cancellationToken);
        var categories = await db.Categories.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.Name).Select(x => new StockOptionDto(x.Id, x.Name, null)).ToListAsync(cancellationToken);
        var locations = await db.InternalLocations.AsNoTracking().Where(x => x.CompanyId == companyId && x.IsActive)
            .OrderBy(x => x.Name).Select(x => new StockOptionDto(x.Id, x.Name, x.WarehouseId)).ToListAsync(cancellationToken);
        return new(products, warehouses, categories, locations);
    }

    private IQueryable<StockBalance> BalanceQuery(Guid companyId) =>
        db.StockBalances.AsNoTracking().Where(balance =>
            balance.CompanyId == companyId &&
            balance.Product!.CompanyId == companyId &&
            balance.Product.ControlsStock &&
            balance.Warehouse!.CompanyId == companyId);

    private static IQueryable<StockBalanceDto> ProjectBalances(IQueryable<StockBalance> query) =>
        query.Select(balance => new StockBalanceDto(balance.Id, balance.ProductId,
            balance.Product!.InternalCode, balance.Product.Name,
            balance.WarehouseId, balance.Warehouse!.Name, balance.QuantityOnHand,
            balance.Product.MinimumStock,
            balance.Product.MinimumStock.HasValue
                ? balance.QuantityOnHand - balance.Product.MinimumStock.Value
                : null,
            balance.Product.MinimumStock.HasValue &&
                balance.QuantityOnHand < balance.Product.MinimumStock.Value,
            balance.QuantityOnHand == 0, balance.LastMovementAt,
            balance.Product.Category != null ? balance.Product.Category.Name : null,
            balance.Product.UnitOfMeasure != null ? balance.Product.UnitOfMeasure.Abbreviation : null));
}
