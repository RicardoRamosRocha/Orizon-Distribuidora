using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Application.Stock;

public sealed record RegisterStockMovementRequest(
    Guid ProductId,
    Guid WarehouseId,
    decimal Quantity,
    string Reason,
    Guid? InternalLocationId = null,
    string? Notes = null,
    decimal? UnitCost = null,
    string? ReferenceType = null,
    string? ReferenceId = null,
    string? DocumentNumber = null,
    string? OperationKey = null,
    DateTimeOffset? OccurredAt = null);

public sealed record StockOperationResult(
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage,
    Guid? MovementId,
    decimal? ResultingQuantity)
{
    public static StockOperationResult Success(Guid movementId, decimal quantity) =>
        new(true, null, null, movementId, quantity);
    public static StockOperationResult Failure(string code, string message) =>
        new(false, code, message, null, null);
}

public sealed record StockBalanceFilter(
    Guid? ProductId = null,
    Guid? WarehouseId = null,
    Guid? CategoryId = null,
    bool OnlyBelowMinimum = false,
    bool OnlyActive = true,
    int Page = 1,
    int PageSize = 25);

public sealed record StockMovementFilter(
    Guid? ProductId = null,
    Guid? WarehouseId = null,
    StockMovementType? Type = null,
    StockMovementDirection? Direction = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? DocumentOrReference = null,
    int Page = 1,
    int PageSize = 25);

public sealed record StockBalanceDto(
    Guid Id, Guid ProductId, string ProductCode, string ProductName,
    Guid WarehouseId, string WarehouseName, decimal CurrentQuantity,
    decimal? MinimumStock, decimal? DifferenceToMinimum,
    bool IsBelowMinimum, bool IsOutOfStock, DateTimeOffset? LastMovementAt);

public sealed record StockMovementDto(
    Guid Id, Guid ProductId, Guid WarehouseId, Guid? InternalLocationId,
    StockMovementType Type, StockMovementDirection Direction, decimal Quantity,
    decimal PreviousQuantity, decimal ResultingQuantity, decimal? UnitCost,
    decimal? TotalCost, string Reason, string? Notes, string? ReferenceType,
    string? ReferenceId, string? DocumentNumber, DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt, Guid? CreatedBy);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record StockLevelIndicators(
    decimal? DifferenceToMinimum, bool IsBelowMinimum, bool IsOutOfStock)
{
    public static StockLevelIndicators Calculate(decimal currentQuantity, decimal? minimumStock, bool controlsStock)
    {
        if (!controlsStock) return new(null, false, false);
        return new(
            minimumStock.HasValue ? currentQuantity - minimumStock.Value : null,
            minimumStock.HasValue && currentQuantity < minimumStock.Value,
            currentQuantity == 0);
    }
}

public interface IStockService
{
    Task<StockOperationResult> RegisterStockEntryAsync(Guid companyId, Guid? userId, RegisterStockMovementRequest request, CancellationToken cancellationToken = default);
    Task<StockOperationResult> RegisterStockIssueAsync(Guid companyId, Guid? userId, RegisterStockMovementRequest request, CancellationToken cancellationToken = default);
    Task<StockOperationResult> RegisterPositiveAdjustmentAsync(Guid companyId, Guid? userId, RegisterStockMovementRequest request, CancellationToken cancellationToken = default);
    Task<StockOperationResult> RegisterNegativeAdjustmentAsync(Guid companyId, Guid? userId, RegisterStockMovementRequest request, CancellationToken cancellationToken = default);
    Task<StockOperationResult> RegisterInitialBalanceAsync(Guid companyId, Guid? userId, RegisterStockMovementRequest request, CancellationToken cancellationToken = default);
    Task<StockBalanceDto?> GetStockBalanceAsync(Guid companyId, Guid productId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<PagedResult<StockBalanceDto>> ListStockBalancesAsync(Guid companyId, StockBalanceFilter filter, CancellationToken cancellationToken = default);
    Task<PagedResult<StockMovementDto>> ListStockMovementsAsync(Guid companyId, StockMovementFilter filter, CancellationToken cancellationToken = default);
}
