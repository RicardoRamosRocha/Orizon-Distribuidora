using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class StockBalance : CompanyOwnedAuditableEntity
{
    private StockBalance() { }

    public StockBalance(Guid companyId, Guid productId, Guid warehouseId)
        : base(companyId)
    {
        ProductId = Required(productId, nameof(productId));
        WarehouseId = Required(warehouseId, nameof(warehouseId));
        ConcurrencyToken = Guid.NewGuid();
    }

    public Guid ProductId { get; private set; }
    public Product? Product { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Warehouse? Warehouse { get; private set; }
    public decimal QuantityOnHand { get; private set; }
    public DateTimeOffset? LastMovementAt { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public ICollection<StockMovement> Movements { get; private set; } = new List<StockMovement>();

    public StockMovement Apply(
        StockMovementType type,
        decimal quantity,
        string reason,
        Guid? internalLocationId = null,
        string? notes = null,
        decimal? unitCost = null,
        string? referenceType = null,
        string? referenceId = null,
        string? documentNumber = null,
        string? operationKey = null,
        Guid? createdBy = null,
        DateTimeOffset? occurredAt = null)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "A quantidade deve ser maior que zero.");
        var direction = DirectionFor(type);
        var previous = QuantityOnHand;
        var resulting = direction == StockMovementDirection.Inbound ? previous + quantity : previous - quantity;
        if (resulting < 0) throw new InvalidOperationException("Saldo insuficiente para a movimentação.");
        if (type is StockMovementType.PositiveAdjustment or StockMovementType.NegativeAdjustment or StockMovementType.InventoryCorrection
            && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("O motivo é obrigatório para ajustes.", nameof(reason));

        var timestamp = occurredAt ?? DateTimeOffset.UtcNow;
        var movement = new StockMovement(CompanyId, Id, ProductId, WarehouseId, internalLocationId,
            type, direction, quantity, previous, resulting, reason, notes, unitCost, referenceType,
            referenceId, documentNumber, operationKey, createdBy, timestamp);
        QuantityOnHand = resulting;
        LastMovementAt = timestamp;
        ConcurrencyToken = Guid.NewGuid();
        Movements.Add(movement);
        return movement;
    }

    private static StockMovementDirection DirectionFor(StockMovementType type) => type switch
    {
        StockMovementType.InitialBalance or StockMovementType.PurchaseReceipt or
        StockMovementType.PositiveAdjustment or StockMovementType.TransferIn or
        StockMovementType.ReturnIn => StockMovementDirection.Inbound,
        StockMovementType.SaleIssue or StockMovementType.NegativeAdjustment or
        StockMovementType.TransferOut or StockMovementType.ReturnOut => StockMovementDirection.Outbound,
        _ => throw new ArgumentException("A correção de inventário exige direção explícita e não está disponível nesta sprint.", nameof(type))
    };

    private static Guid Required(Guid value, string name) =>
        value == Guid.Empty ? throw new ArgumentException("O identificador é obrigatório.", name) : value;
}
