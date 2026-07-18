using Orizon.Distribuidora.Domain.Common;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class PriceTable : CompanyOwnedAuditableEntity
{
    private readonly List<ProductPrice> _productPrices = [];

    private PriceTable()
    {
    }

    public PriceTable(
        Guid companyId,
        string name,
        string? description = null,
        bool isDefault = false)
        : base(companyId)
    {
        Name = NormalizeRequired(name, nameof(name));
        Description = NormalizeOptional(description);
        IsDefault = isDefault;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<ProductPrice> ProductPrices => _productPrices;

    private static string NormalizeRequired(string value, string parameterName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("O nome da tabela de preços é obrigatório.", parameterName)
            : value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
