using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Tests.Entities;

public sealed class ProductTests
{
    [Fact]
    public void OwnProduct_ShouldAllowStockControlAndCalculateMargin()
    {
        var product = CreateProduct(ProductType.Own);

        product.Update(
            "abc-1", "sku-1", "789", "ref",
            "Cimento", "Cimento CP II", "Descrição",
            ProductType.Own, true, true,
            null, null, null, Guid.NewGuid(), null, null, null,
            Guid.NewGuid(), Guid.NewGuid(), "12.345.678", "12.345-67",
            60, 100, CommissionType.Percentage, 5, DateOnly.FromDateTime(DateTime.UtcNow),
            10, null);

        Assert.True(product.ControlsStock);
        Assert.Equal(10, product.MinimumStock);
        Assert.Equal("12345678", product.Ncm);
        Assert.Equal("1234567", product.Cest);
        Assert.Equal(40, product.CalculateMarginPercentage());
    }

    [Fact]
    public void ThirdPartyProduct_ShouldRequirePartner()
    {
        var product = CreateProduct(ProductType.Own);

        var action = () => product.Update(
            "TER-1", null, null, null,
            "Produto terceiro", null, null,
            ProductType.ThirdParty, true, true,
            null, null, null, Guid.NewGuid(), null, null, null,
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            1, 2, null, null, null, 1, null);

        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void ThirdPartyProduct_ShouldDisableStockWhenPartnerExists()
    {
        var product = CreateProduct(ProductType.Own);

        product.Update(
            "TER-2", null, null, null,
            "Produto terceiro", null, null,
            ProductType.ThirdParty, true, true,
            null, null, null, Guid.NewGuid(), null, null, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            1, 2, null, null, null, 1, null);

        Assert.False(product.ControlsStock);
        Assert.Null(product.MinimumStock);
        Assert.Null(product.DefaultWarehouseId);
        Assert.Null(product.DefaultWarehouseLocationId);
    }

    [Fact]
    public void Service_ShouldDisableStock()
    {
        var product = CreateProduct(ProductType.Own);

        product.Update(
            "SER-1", null, null, null,
            "Serviço", null, null,
            ProductType.Service, true, true,
            null, null, null, Guid.NewGuid(), null, null, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), null, null,
            1, 2, null, null, null, 1, null);

        Assert.False(product.ControlsStock);
        Assert.Null(product.MinimumStock);
    }

    [Fact]
    public void MadeToOrder_ShouldAllowOptionalStockControl()
    {
        var product = CreateProduct(ProductType.Own);

        product.Update(
            "ENC-1", null, null, null,
            "Sob encomenda", null, null,
            ProductType.MadeToOrder, false, true,
            null, null, null, Guid.NewGuid(), null, null, null,
            null, null, null, null,
            1, 2, null, null, null, null, null);

        Assert.False(product.ControlsStock);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Product_ShouldRejectInvalidNumbers(decimal cost, decimal price, decimal stock)
    {
        var product = CreateProduct(ProductType.Own);

        var action = () => product.Update(
            "NUM-1", null, null, null,
            "Produto", null, null,
            ProductType.Own, true, true,
            null, null, null, Guid.NewGuid(), null, null, null,
            null, null, null, null,
            cost, price, null, null, null, stock, null);

        Assert.ThrowsAny<Exception>(action);
    }

    [Fact]
    public void Product_ShouldRejectPercentageCommissionAboveOneHundred()
    {
        var product = CreateProduct(ProductType.Own);

        var action = () => product.Update(
            "COM-1", null, null, null,
            "Produto", null, null,
            ProductType.Own, false, true,
            null, null, null, Guid.NewGuid(), null, null, null,
            null, null, null, null,
            0, 0, CommissionType.Percentage, 101, null, null, null);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Theory]
    [InlineData("1234567", null)]
    [InlineData(null, "123456")]
    public void Product_ShouldRejectInvalidFiscalCodes(string? ncm, string? cest)
    {
        var product = CreateProduct(ProductType.Own);

        var action = () => product.Update(
            "FIS-1", null, null, null,
            "Produto", null, null,
            ProductType.Own, false, true,
            null, null, null, Guid.NewGuid(), null, null, null,
            null, null, ncm, cest,
            0, 0, null, null, null, null, null);

        Assert.Throws<ArgumentException>(action);
    }

    private static Product CreateProduct(ProductType type)
    {
        return new Product(Guid.NewGuid(), "PRD-1", "Produto teste", Guid.NewGuid(), type);
    }
}
