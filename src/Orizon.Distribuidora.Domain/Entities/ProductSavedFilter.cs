using Orizon.Distribuidora.Domain.Common;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class ProductSavedFilter : CompanyOwnedAuditableEntity
{
    private ProductSavedFilter() { }

    public ProductSavedFilter(Guid companyId, Guid userId, string name, string filterJson) : base(companyId)
    {
        UserId = userId;
        Rename(name);
        SetFilter(filterJson);
    }

    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string FilterJson { get; private set; } = "{}";

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
            throw new ArgumentException("Nome do filtro invalido.", nameof(name));
        Name = name.Trim();
    }

    public void SetFilter(string filterJson)
    {
        if (string.IsNullOrWhiteSpace(filterJson) || filterJson.Length > 10_000)
            throw new ArgumentException("Filtro invalido.", nameof(filterJson));
        FilterJson = filterJson;
    }
}
