using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Domain.Services;

namespace Orizon.Distribuidora.Domain.Tests.Entities;

public sealed class BasicRegistrationTests
{
    [Fact]
    public void Category_ShouldBeCreatedActiveForCompany()
    {
        var companyId = Guid.NewGuid();

        var category = new Category(
            companyId,
            "Argamassas",
            "ARG",
            "Materiais para assentamento");

        Assert.Equal(companyId, category.CompanyId);
        Assert.Equal("Argamassas", category.Name);
        Assert.Equal("ARG", category.Code);
        Assert.True(category.IsActive);
    }

    [Fact]
    public void Subcategory_ShouldRequireCategory()
    {
        var action = () => new Subcategory(
            Guid.NewGuid(),
            Guid.Empty,
            "AC-I",
            "ACI",
            null);

        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void UnitOfMeasure_ShouldRejectInvalidDecimalPlaces(int decimalPlaces)
    {
        var action = () => new UnitOfMeasure(
            Guid.NewGuid(),
            "Quilograma",
            "KG",
            null,
            decimalPlaces,
            allowsFraction: true);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void UnitOfMeasure_ShouldUseZeroDecimalPlaces_WhenDoesNotAllowFraction()
    {
        var unit = new UnitOfMeasure(
            Guid.NewGuid(),
            "Unidade",
            "UN",
            null,
            3,
            allowsFraction: false);

        Assert.Equal(0, unit.DecimalPlaces);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void CommercialPartner_ShouldRejectInvalidCommission(decimal commission)
    {
        var action = () => new CommercialPartner(
            Guid.NewGuid(),
            PersonType.Company,
            "Parceiro Sul",
            null,
            commission);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void CommercialPartner_ShouldAcceptValidCommission()
    {
        var partner = new CommercialPartner(
            Guid.NewGuid(),
            PersonType.Company,
            "Representante Centro",
            null,
            12.5m);

        Assert.Equal(12.5m, partner.CommissionPercentage);
    }

    [Fact]
    public void CommercialPartner_ShouldRejectInvalidEmail()
    {
        var partner = new CommercialPartner(
            Guid.NewGuid(),
            PersonType.Company,
            "Representante Centro",
            null,
            0);

        var action = () => partner.Update(
            PersonType.Company,
            "Representante Centro",
            null,
            "email-invalido",
            null,
            null,
            0,
            null);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Warehouse_ShouldAllowOnlyExplicitDefaultStateChanges()
    {
        var warehouse = new Warehouse(
            Guid.NewGuid(),
            "Principal",
            "DEP-01",
            null,
            isDefault: true);

        warehouse.UnmarkAsDefault();

        Assert.False(warehouse.IsDefault);
    }

    [Fact]
    public void InternalLocation_ShouldRequireWarehouse()
    {
        var action = () => new InternalLocation(
            Guid.NewGuid(),
            Guid.Empty,
            "A1-01",
            "Corredor A",
            null);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Supplier_ShouldRejectInvalidDocument()
    {
        var action = () => new Supplier(
            Guid.NewGuid(),
            PersonType.Company,
            "Fornecedor Inválido",
            null,
            "123");

        Assert.Throws<ArgumentException>(action);
    }

    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("04.252.011/0001-10")]
    public void DocumentValidator_ShouldAcceptValidCpfOrCnpj(string document)
    {
        var normalized = DocumentValidator.Normalize(document);

        Assert.True(DocumentValidator.IsValidCpfOrCnpj(normalized));
    }

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("00.000.000/0000-00")]
    public void DocumentValidator_ShouldRejectInvalidCpfOrCnpj(string document)
    {
        var normalized = DocumentValidator.Normalize(document);

        Assert.False(DocumentValidator.IsValidCpfOrCnpj(normalized));
    }

    [Fact]
    public void RegistrationStatus_ShouldProtectSystemStatusFromDelete()
    {
        var status = new RegistrationStatus(
            Guid.NewGuid(),
            "Ativo",
            "ATIVO",
            null,
            "#16A34A",
            1,
            isSystem: true);

        Assert.Throws<InvalidOperationException>(status.EnsureCanDelete);
    }

    [Fact]
    public void RegistrationStatus_ShouldRejectInvalidColor()
    {
        var action = () => new RegistrationStatus(
            Guid.NewGuid(),
            "Bloqueado",
            "BLOQUEADO",
            null,
            "red",
            1,
            isSystem: false);

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void RegistrationStatus_ShouldRejectNegativeSortOrder()
    {
        var action = () => new RegistrationStatus(
            Guid.NewGuid(),
            "Bloqueado",
            "BLOQUEADO",
            null,
            "#DC2626",
            -1,
            isSystem: false);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }
}
