using Orizon.Distribuidora.Domain.Common;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class ProductPrice : CompanyOwnedAuditableEntity
{
    private readonly List<PriceHistory> _priceHistories = [];

    private ProductPrice()
    {
    }

    public ProductPrice(
        Guid companyId,
        Guid productId,
        Guid priceTableId,
        decimal costPrice,
        decimal salePrice,
        decimal? promotionalPrice = null,
        decimal? minimumMargin = null,
        decimal? currentMargin = null,
        decimal? markup = null,
        DateTimeOffset? promotionStartDate = null,
        DateTimeOffset? promotionEndDate = null,
        bool isPromotionActive = false)
        : base(companyId)
    {
        ProductId = RequiredId(productId, nameof(productId), "O produto é obrigatório.");
        PriceTableId = RequiredId(priceTableId, nameof(priceTableId), "A tabela de preços é obrigatória.");
        CostPrice = NonNegative(costPrice, nameof(costPrice));
        SalePrice = NonNegative(salePrice, nameof(salePrice));
        PromotionalPrice = OptionalNonNegative(promotionalPrice, nameof(promotionalPrice));
        MinimumMargin = minimumMargin;
        CurrentMargin = currentMargin;
        Markup = markup;
        PromotionStartDate = promotionStartDate;
        PromotionEndDate = promotionEndDate;
        IsPromotionActive = isPromotionActive;
    }

    public Guid ProductId { get; private set; }

    public Product? Product { get; private set; }

    public Guid PriceTableId { get; private set; }

    public PriceTable? PriceTable { get; private set; }

    public decimal CostPrice { get; private set; }

    public decimal SalePrice { get; private set; }

    public decimal? PromotionalPrice { get; private set; }

    public decimal? MinimumMargin { get; private set; }

    public decimal? CurrentMargin { get; private set; }

    public decimal? Markup { get; private set; }

    public DateTimeOffset? PromotionStartDate { get; private set; }

    public DateTimeOffset? PromotionEndDate { get; private set; }

    public bool IsPromotionActive { get; private set; }

    public ICollection<PriceHistory> PriceHistories => _priceHistories;

    private static Guid RequiredId(Guid value, string parameterName, string message)
    {
        return value == Guid.Empty ? throw new ArgumentException(message, parameterName) : value;
    }

    private static decimal NonNegative(decimal value, string parameterName)
    {
        return value < 0
            ? throw new ArgumentOutOfRangeException(parameterName, "O preço não pode ser negativo.")
            : value;
    }

    private static decimal? OptionalNonNegative(decimal? value, string parameterName)
    {
        return value < 0
            ? throw new ArgumentOutOfRangeException(parameterName, "O preço não pode ser negativo.")
            : value;
    }
}
