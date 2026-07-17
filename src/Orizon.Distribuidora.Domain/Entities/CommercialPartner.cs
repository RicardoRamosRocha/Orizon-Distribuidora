using System.Text.RegularExpressions;
using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Domain.Services;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class CommercialPartner : CompanyOwnedAuditableEntity
{
    private CommercialPartner()
    {
    }

    public CommercialPartner(
        Guid companyId,
        PersonType type,
        string name,
        string? document,
        decimal commissionPercentage)
        : base(companyId)
    {
        Type = type;
        SetName(name);
        SetDocument(document);
        SetCommissionPercentage(commissionPercentage);
        IsActive = true;
    }

    public PersonType Type { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Document { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? ContactName { get; private set; }

    public decimal CommissionPercentage { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; } = true;

    public void Update(
        PersonType type,
        string name,
        string? document,
        string? email,
        string? phone,
        string? contactName,
        decimal commissionPercentage,
        string? notes)
    {
        Type = type;
        SetName(name);
        SetDocument(document);
        SetEmail(email);
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        ContactName = string.IsNullOrWhiteSpace(contactName) ? null : contactName.Trim();
        SetCommissionPercentage(commissionPercentage);
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "O nome é obrigatório.",
                nameof(name));
        }

        Name = name.Trim();
    }

    private void SetDocument(string? document)
    {
        var normalized = DocumentValidator.Normalize(document);

        if (string.IsNullOrEmpty(normalized))
        {
            Document = null;
            return;
        }

        if (!DocumentValidator.IsValidCpfOrCnpj(normalized))
        {
            throw new ArgumentException(
                "Informe um CPF ou CNPJ válido.",
                nameof(document));
        }

        Document = normalized;
    }

    private void SetEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            Email = null;
            return;
        }

        var normalized = email.Trim();

        if (!Regex.IsMatch(normalized, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ArgumentException(
                "Informe um e-mail válido.",
                nameof(email));
        }

        Email = normalized;
    }

    private void SetCommissionPercentage(decimal commissionPercentage)
    {
        if (commissionPercentage is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(commissionPercentage),
                "O percentual de comissão deve estar entre 0 e 100.");
        }

        CommissionPercentage = commissionPercentage;
    }
}
