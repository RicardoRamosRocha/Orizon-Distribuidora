using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Tests.Entities;

public sealed class PriceManagementTests
{
    [Fact]
    public void PriceTable_ShouldInitializeAsActiveForCompany()
    {
        var companyId = Guid.NewGuid();

        var priceTable = new PriceTable(companyId, "  Atacado  ", "  Clientes atacadistas  ", true);

        Assert.Equal(companyId, priceTable.CompanyId);
        Assert.Equal("Atacado", priceTable.Name);
        Assert.Equal("Clientes atacadistas", priceTable.Description);
        Assert.True(priceTable.IsDefault);
        Assert.True(priceTable.IsActive);
        Assert.Empty(priceTable.ProductPrices);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(1, -10)]
    public void ProductPrice_ShouldRejectNegativePrices(
        decimal costPrice,
        decimal salePrice)
    {
        var action = () => new ProductPrice(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            costPrice,
            salePrice);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void ProductPrice_ShouldRejectNegativePromotionalPrice()
    {
        var action = () => new ProductPrice(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            10,
            -5);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void PriceHistory_ShouldRegisterOriginAndTimestamp()
    {
        var changedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var userId = Guid.NewGuid();

        var history = new PriceHistory(
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            12,
            20,
            24,
            50,
            50,
            4,
            20,
            " Reposicionamento de mercado ",
            userId,
            PriceChangeOrigin.Manual,
            changedAt);

        Assert.Equal("Reposicionamento de mercado", history.ChangeReason);
        Assert.Equal(userId, history.ChangedByUserId);
        Assert.Equal(PriceChangeOrigin.Manual, history.Origin);
        Assert.Equal(changedAt, history.ChangedAt);
        Assert.Null(history.ProductPrice);
    }
}
