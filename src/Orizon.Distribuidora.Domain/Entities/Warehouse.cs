namespace Orizon.Distribuidora.Domain.Entities;

public sealed class Warehouse : BasicRegistrationEntity
{
    private Warehouse()
    {
    }

    public Warehouse(
        Guid companyId,
        string name,
        string code,
        string? description,
        bool isDefault)
        : base(companyId, name, code, description)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "O código é obrigatório.",
                nameof(code));
        }

        IsDefault = isDefault;
    }

    public bool IsDefault { get; private set; }

    public ICollection<InternalLocation> Locations { get; private set; } =
        new List<InternalLocation>();

    public void Update(
        string name,
        string code,
        string? description,
        bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "O código é obrigatório.",
                nameof(code));
        }

        Update(name, code, description);
        IsDefault = isDefault;
    }

    public void MarkAsDefault()
    {
        IsDefault = true;
    }

    public void UnmarkAsDefault()
    {
        IsDefault = false;
    }
}
