using System.Text.RegularExpressions;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class RegistrationStatus : BasicRegistrationEntity
{
    private RegistrationStatus()
    {
    }

    public RegistrationStatus(
        Guid companyId,
        string name,
        string code,
        string? description,
        string? color,
        int sortOrder,
        bool isSystem)
        : base(companyId, name, code, description)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException(
                "O código é obrigatório.",
                nameof(code));
        }

        SetColor(color);
        SetSortOrder(sortOrder);
        IsSystem = isSystem;
    }

    public string? Color { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsSystem { get; private set; }

    public void Update(
        string name,
        string code,
        string? description,
        string? color,
        int sortOrder)
    {
        Update(name, code, description);
        SetColor(color);
        SetSortOrder(sortOrder);
    }

    public void EnsureCanDelete()
    {
        if (IsSystem)
        {
            throw new InvalidOperationException(
                "Status de sistema não pode ser excluído.");
        }
    }

    private void SetColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            Color = null;
            return;
        }

        var normalized = color.Trim();

        if (!Regex.IsMatch(normalized, "^#[0-9A-Fa-f]{6}$"))
        {
            throw new ArgumentException(
                "Informe uma cor hexadecimal no formato #RRGGBB.",
                nameof(color));
        }

        Color = normalized.ToUpperInvariant();
    }

    private void SetSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "A ordem deve ser igual ou maior que zero.");
        }

        SortOrder = sortOrder;
    }
}
