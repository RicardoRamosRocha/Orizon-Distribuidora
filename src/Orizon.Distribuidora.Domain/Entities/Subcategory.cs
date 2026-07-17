namespace Orizon.Distribuidora.Domain.Entities;

public sealed class Subcategory : BasicRegistrationEntity
{
    private Subcategory()
    {
    }

    public Subcategory(
        Guid companyId,
        Guid categoryId,
        string name,
        string? code,
        string? description)
        : base(companyId, name, code, description)
    {
        SetCategory(categoryId);
    }

    public Guid CategoryId { get; private set; }

    public Category? Category { get; private set; }

    public void Update(
        Guid categoryId,
        string name,
        string? code,
        string? description)
    {
        SetCategory(categoryId);
        Update(name, code, description);
    }

    private void SetCategory(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "A categoria é obrigatória.",
                nameof(categoryId));
        }

        CategoryId = categoryId;
    }
}
