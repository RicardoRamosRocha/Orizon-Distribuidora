using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class PriceHistory : CompanyOwnedAuditableEntity
{
    private PriceHistory()
    {
    }

    public PriceHistory(
        Guid companyId,
        Guid productPriceId,
        decimal previousCostPrice,
        decimal newCostPrice,
        decimal previousSalePrice,
        decimal newSalePrice,
        decimal? previousMargin,
        decimal? newMargin,
        decimal differenceValue,
        decimal differencePercent,
        string changeReason,
        Guid? changedByUserId,
        PriceChangeOrigin origin,
        DateTimeOffset? changedAt = null)
        : base(companyId)
    {
        ProductPriceId = productPriceId == Guid.Empty
            ? throw new ArgumentException("O preço do produto é obrigatório.", nameof(productPriceId))
            : productPriceId;
        PreviousCostPrice = previousCostPrice;
        NewCostPrice = newCostPrice;
        PreviousSalePrice = previousSalePrice;
        NewSalePrice = newSalePrice;
        PreviousMargin = previousMargin;
        NewMargin = newMargin;
        DifferenceValue = differenceValue;
        DifferencePercent = differencePercent;
        ChangeReason = string.IsNullOrWhiteSpace(changeReason)
            ? throw new ArgumentException("O motivo da alteração é obrigatório.", nameof(changeReason))
            : changeReason.Trim();
        ChangedAt = changedAt ?? DateTimeOffset.UtcNow;
        ChangedByUserId = changedByUserId;
        Origin = origin;
    }

    public Guid ProductPriceId { get; private set; }

    public ProductPrice? ProductPrice { get; private set; }

    public decimal PreviousCostPrice { get; private set; }

    public decimal NewCostPrice { get; private set; }

    public decimal PreviousSalePrice { get; private set; }

    public decimal NewSalePrice { get; private set; }

    public decimal? PreviousMargin { get; private set; }

    public decimal? NewMargin { get; private set; }

    public decimal DifferenceValue { get; private set; }

    public decimal DifferencePercent { get; private set; }

    public string ChangeReason { get; private set; } = string.Empty;

    public DateTimeOffset ChangedAt { get; private set; }

    public Guid? ChangedByUserId { get; private set; }

    public PriceChangeOrigin Origin { get; private set; }
}
