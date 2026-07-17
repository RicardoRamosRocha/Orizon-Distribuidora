namespace Orizon.Distribuidora.Domain.Entities;

public sealed class InternalLocation : BasicRegistrationEntity
{
    private InternalLocation()
    {
    }

    public InternalLocation(
        Guid companyId,
        Guid warehouseId,
        string code,
        string name,
        string? description)
        : base(companyId, name, code, description)
    {
        SetWarehouse(warehouseId);

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "O código é obrigatório.",
                nameof(code));
        }
    }

    public Guid WarehouseId { get; private set; }

    public Warehouse? Warehouse { get; private set; }

    public string? Aisle { get; private set; }

    public string? Section { get; private set; }

    public string? Shelf { get; private set; }

    public string? Position { get; private set; }

    public void Update(
        Guid warehouseId,
        string code,
        string name,
        string? description,
        string? aisle,
        string? section,
        string? shelf,
        string? position)
    {
        SetWarehouse(warehouseId);
        Update(name, code, description);
        Aisle = NormalizeOptional(aisle);
        Section = NormalizeOptional(section);
        Shelf = NormalizeOptional(shelf);
        Position = NormalizeOptional(position);
    }

    private void SetWarehouse(Guid warehouseId)
    {
        if (warehouseId == Guid.Empty)
        {
            throw new ArgumentException(
                "O depósito é obrigatório.",
                nameof(warehouseId));
        }

        WarehouseId = warehouseId;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
