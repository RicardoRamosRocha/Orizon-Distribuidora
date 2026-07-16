using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class Company : AuditableEntity
{
    private Company()
    {
    }

    public Company(
        string legalName,
        string tradeName,
        string document,
        string slug)
    {
        SetLegalName(legalName);
        SetTradeName(tradeName);
        SetDocument(document);
        SetSlug(slug);

        Status = CompanyStatus.Active;
    }

    public string LegalName { get; private set; } = string.Empty;

    public string TradeName { get; private set; } = string.Empty;

    public string Document { get; private set; } = string.Empty;

    public string Slug { get; private set; } = string.Empty;

    public CompanyStatus Status { get; private set; }

    public void Update(
        string legalName,
        string tradeName,
        string document)
    {
        SetLegalName(legalName);
        SetTradeName(tradeName);
        SetDocument(document);
    }

    public void Activate()
    {
        Status = CompanyStatus.Active;
    }

    public void Suspend()
    {
        Status = CompanyStatus.Suspended;
    }

    public void Deactivate()
    {
        Status = CompanyStatus.Inactive;
    }

    private void SetLegalName(string legalName)
    {
        if (string.IsNullOrWhiteSpace(legalName))
        {
            throw new ArgumentException(
                "A razão social é obrigatória.",
                nameof(legalName));
        }

        LegalName = legalName.Trim();
    }

    private void SetTradeName(string tradeName)
    {
        if (string.IsNullOrWhiteSpace(tradeName))
        {
            throw new ArgumentException(
                "O nome fantasia é obrigatório.",
                nameof(tradeName));
        }

        TradeName = tradeName.Trim();
    }

    private void SetDocument(string document)
    {
        if (string.IsNullOrWhiteSpace(document))
        {
            throw new ArgumentException(
                "O documento da empresa é obrigatório.",
                nameof(document));
        }

        Document = document.Trim();
    }

    private void SetSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException(
                "O identificador da empresa é obrigatório.",
                nameof(slug));
        }

        Slug = slug
            .Trim()
            .ToLowerInvariant()
            .Replace(" ", "-");
    }
}
