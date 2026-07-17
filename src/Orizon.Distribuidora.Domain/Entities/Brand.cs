namespace Orizon.Distribuidora.Domain.Entities;

public sealed class Brand : BasicRegistrationEntity
{
    private Brand()
    {
    }

    public Brand(
        Guid companyId,
        string name,
        string? code,
        string? description)
        : base(companyId, name, code, description)
    {
    }
}
