using Orizon.Distribuidora.Domain.Common;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class ProductChangeHistory : CompanyOwnedAuditableEntity
{
    private ProductChangeHistory()
    {
    }

    public ProductChangeHistory(
        Guid companyId,
        Guid productId,
        string fieldName,
        string? oldValue,
        string? newValue,
        string origin)
        : base(companyId)
    {
        ProductId = productId == Guid.Empty
            ? throw new ArgumentException("O produto é obrigatório.", nameof(productId))
            : productId;
        FieldName = string.IsNullOrWhiteSpace(fieldName)
            ? throw new ArgumentException("O campo alterado é obrigatório.", nameof(fieldName))
            : fieldName.Trim();
        OldValue = string.IsNullOrWhiteSpace(oldValue) ? null : oldValue.Trim();
        NewValue = string.IsNullOrWhiteSpace(newValue) ? null : newValue.Trim();
        Origin = string.IsNullOrWhiteSpace(origin) ? "manual" : origin.Trim();
    }

    public Guid ProductId { get; private set; }

    public Product? Product { get; private set; }

    public string FieldName { get; private set; } = string.Empty;

    public string? OldValue { get; private set; }

    public string? NewValue { get; private set; }

    public string Origin { get; private set; } = string.Empty;
}
