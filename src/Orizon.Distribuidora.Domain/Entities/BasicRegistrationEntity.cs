using Orizon.Distribuidora.Domain.Common;

namespace Orizon.Distribuidora.Domain.Entities;

public abstract class BasicRegistrationEntity : CompanyOwnedAuditableEntity
{
    protected BasicRegistrationEntity()
    {
    }

    protected BasicRegistrationEntity(
        Guid companyId,
        string name,
        string? code,
        string? description)
        : base(companyId)
    {
        SetName(name);
        SetCode(code);
        SetDescription(description);
        IsActive = true;
    }

    public string Name { get; protected set; } = string.Empty;

    public string? Code { get; protected set; }

    public string? Description { get; protected set; }

    public bool IsActive { get; protected set; } = true;

    public virtual void Update(
        string name,
        string? code,
        string? description)
    {
        SetName(name);
        SetCode(code);
        SetDescription(description);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public virtual void Deactivate()
    {
        IsActive = false;
    }

    protected void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "O nome é obrigatório.",
                nameof(name));
        }

        Name = name.Trim();
    }

    protected void SetCode(string? code)
    {
        Code = string.IsNullOrWhiteSpace(code)
            ? null
            : code.Trim().ToUpperInvariant();
    }

    protected void SetDescription(string? description)
    {
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}
