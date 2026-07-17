namespace Orizon.Distribuidora.Domain.Entities;

public sealed class Category : BasicRegistrationEntity
{
    private Category()
    {
    }

    public Category(
        Guid companyId,
        string name,
        string? code,
        string? description)
        : base(companyId, name, code, description)
    {
    }
}
