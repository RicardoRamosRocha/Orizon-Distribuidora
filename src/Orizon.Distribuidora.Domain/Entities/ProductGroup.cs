namespace Orizon.Distribuidora.Domain.Entities;

public sealed class ProductGroup : BasicRegistrationEntity
{
    private ProductGroup()
    {
    }

    public ProductGroup(
        Guid companyId,
        string name,
        string? code,
        string? description)
        : base(companyId, name, code, description)
    {
    }
}
