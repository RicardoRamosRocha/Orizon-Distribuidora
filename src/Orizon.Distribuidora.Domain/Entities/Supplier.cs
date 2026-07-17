using System.Text.RegularExpressions;
using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Domain.Services;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class Supplier : CompanyOwnedAuditableEntity
{
    private Supplier()
    {
    }

    public Supplier(
        Guid companyId,
        PersonType type,
        string legalName,
        string? tradeName,
        string? document)
        : base(companyId)
    {
        Type = type;
        SetLegalName(legalName);
        SetTradeName(tradeName);
        SetDocument(document);
        IsActive = true;
    }

    public PersonType Type { get; private set; }

    public string LegalName { get; private set; } = string.Empty;

    public string? TradeName { get; private set; }

    public string? Document { get; private set; }

    public string? StateRegistration { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? MobilePhone { get; private set; }

    public string? ContactName { get; private set; }

    public string? ZipCode { get; private set; }

    public string? Street { get; private set; }

    public string? Number { get; private set; }

    public string? Complement { get; private set; }

    public string? Neighborhood { get; private set; }

    public string? City { get; private set; }

    public string? State { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; } = true;

    public void Update(
        PersonType type,
        string legalName,
        string? tradeName,
        string? document,
        string? stateRegistration,
        string? email,
        string? phone,
        string? mobilePhone,
        string? contactName,
        string? zipCode,
        string? street,
        string? number,
        string? complement,
        string? neighborhood,
        string? city,
        string? state,
        string? notes)
    {
        Type = type;
        SetLegalName(legalName);
        SetTradeName(tradeName);
        SetDocument(document);
        StateRegistration = NormalizeOptional(stateRegistration);
        SetEmail(email);
        Phone = NormalizeOptional(phone);
        MobilePhone = NormalizeOptional(mobilePhone);
        ContactName = NormalizeOptional(contactName);
        ZipCode = NormalizeOptional(zipCode);
        Street = NormalizeOptional(street);
        Number = NormalizeOptional(number);
        Complement = NormalizeOptional(complement);
        Neighborhood = NormalizeOptional(neighborhood);
        City = NormalizeOptional(city);
        SetState(state);
        Notes = NormalizeOptional(notes);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private void SetLegalName(string legalName)
    {
        if (string.IsNullOrWhiteSpace(legalName))
        {
            throw new ArgumentException(
                "A razão social/nome é obrigatória.",
                nameof(legalName));
        }

        LegalName = legalName.Trim();
    }

    private void SetTradeName(string? tradeName)
    {
        TradeName = NormalizeOptional(tradeName);
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

    private void SetState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            State = null;
            return;
        }

        if (state.Trim().Length != 2)
        {
            throw new ArgumentException(
                "A UF deve possuir dois caracteres.",
                nameof(state));
        }

        State = state.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
