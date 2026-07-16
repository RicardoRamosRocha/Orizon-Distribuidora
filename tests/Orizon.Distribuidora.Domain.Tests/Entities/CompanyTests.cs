using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Tests.Entities;

public sealed class CompanyTests
{
    [Fact]
    public void Constructor_ShouldCreateActiveCompany()
    {
        var company = new Company(
            "Orizon Distribuidora Ltda",
            "Orizon Distribuidora",
            "12345678000199",
            "Orizon Distribuidora");

        Assert.NotEqual(Guid.Empty, company.Id);
        Assert.Equal("Orizon Distribuidora Ltda", company.LegalName);
        Assert.Equal("Orizon Distribuidora", company.TradeName);
        Assert.Equal("orizon-distribuidora", company.Slug);
        Assert.Equal(CompanyStatus.Active, company.Status);
        Assert.False(company.IsDeleted);
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyLegalName()
    {
        var action = () => new Company(
            string.Empty,
            "Orizon Distribuidora",
            "12345678000199",
            "orizon-distribuidora");

        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Suspend_ShouldChangeCompanyStatus()
    {
        var company = new Company(
            "Orizon Distribuidora Ltda",
            "Orizon Distribuidora",
            "12345678000199",
            "orizon-distribuidora");

        company.Suspend();

        Assert.Equal(CompanyStatus.Suspended, company.Status);
    }
}
