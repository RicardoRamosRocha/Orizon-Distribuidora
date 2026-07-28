using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class StockMovement : Entity
{
    private StockMovement() { }

    internal StockMovement(
        Guid companyId,
        Guid stockBalanceId,
        Guid productId,
        Guid warehouseId,
        Guid? internalLocationId,
        StockMovementType type,
        StockMovementDirection direction,
        decimal quantity,
        decimal previousQuantity,
        decimal resultingQuantity,
        string reason,
        string? notes,
        decimal? unitCost,
        string? referenceType,
        string? referenceId,
        string? documentNumber,
        string? operationKey,
        Guid? createdBy,
        DateTimeOffset occurredAt)
    {
        CompanyId = Required(companyId, nameof(companyId));
        StockBalanceId = Required(stockBalanceId, nameof(stockBalanceId));
        ProductId = Required(productId, nameof(productId));
        WarehouseId = Required(warehouseId, nameof(warehouseId));
        if (internalLocationId == Guid.Empty) throw new ArgumentException("A localização é inválida.", nameof(internalLocationId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "A quantidade deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("O motivo é obrigatório.", nameof(reason));

        InternalLocationId = internalLocationId;
        Type = type;
        Direction = direction;
        Quantity = quantity;
        PreviousQuantity = previousQuantity;
        ResultingQuantity = resultingQuantity;
        Reason = reason.Trim();
        Notes = Normalize(notes);
        UnitCost = unitCost is < 0 ? throw new ArgumentOutOfRangeException(nameof(unitCost)) : unitCost;
        ReferenceType = Normalize(referenceType);
        ReferenceId = Normalize(referenceId);
        DocumentNumber = Normalize(documentNumber);
        OperationKey = Normalize(operationKey);
        CreatedBy = createdBy;
        OccurredAt = occurredAt;
        CreatedAt = DateTimeOffset.UtcNow;

        var expected = direction == StockMovementDirection.Inbound
            ? previousQuantity + quantity
            : previousQuantity - quantity;
        if (expected != resultingQuantity)
            throw new ArgumentException("As quantidades anterior e resultante são incoerentes.");
    }

    public Guid CompanyId { get; private set; }
    public Guid StockBalanceId { get; private set; }
    public StockBalance? StockBalance { get; private set; }
    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Warehouse? Warehouse { get; private set; }
    public Guid? InternalLocationId { get; private set; }
    public InternalLocation? InternalLocation { get; private set; }
    public StockMovementType Type { get; private set; }
    public StockMovementDirection Direction { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal PreviousQuantity { get; private set; }
    public decimal ResultingQuantity { get; private set; }
    public decimal? UnitCost { get; private set; }
    public decimal? TotalCost => UnitCost * Quantity;
    public string Reason { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }
    public string? DocumentNumber { get; private set; }
    public string? OperationKey { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }

    private static Guid Required(Guid value, string name) =>
        value == Guid.Empty ? throw new ArgumentException("O identificador é obrigatório.", name) : value;
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
