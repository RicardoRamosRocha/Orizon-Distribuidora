namespace Orizon.Distribuidora.Domain.Entities;

public sealed class UnitOfMeasure : BasicRegistrationEntity
{
    private UnitOfMeasure()
    {
    }

    public UnitOfMeasure(
        Guid companyId,
        string name,
        string abbreviation,
        string? description,
        int decimalPlaces,
        bool allowsFraction)
        : base(companyId, name, abbreviation, description)
    {
        SetAbbreviation(abbreviation);
        SetPrecision(decimalPlaces, allowsFraction);
    }

    public string Abbreviation { get; private set; } = string.Empty;

    public int DecimalPlaces { get; private set; }

    public bool AllowsFraction { get; private set; }

    public void Update(
        string name,
        string abbreviation,
        string? description,
        int decimalPlaces,
        bool allowsFraction)
    {
        SetName(name);
        SetAbbreviation(abbreviation);
        SetCode(abbreviation);
        SetDescription(description);
        SetPrecision(decimalPlaces, allowsFraction);
    }

    private void SetAbbreviation(string abbreviation)
    {
        if (string.IsNullOrWhiteSpace(abbreviation))
        {
            throw new ArgumentException(
                "A sigla é obrigatória.",
                nameof(abbreviation));
        }

        Abbreviation = abbreviation.Trim().ToUpperInvariant();
    }

    private void SetPrecision(
        int decimalPlaces,
        bool allowsFraction)
    {
        if (decimalPlaces is < 0 or > 6)
        {
            throw new ArgumentOutOfRangeException(
                nameof(decimalPlaces),
                "As casas decimais devem estar entre 0 e 6.");
        }

        AllowsFraction = allowsFraction;
        DecimalPlaces = allowsFraction ? decimalPlaces : 0;
    }
}
