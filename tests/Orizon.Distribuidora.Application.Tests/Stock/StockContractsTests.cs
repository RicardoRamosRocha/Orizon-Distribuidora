using Orizon.Distribuidora.Application.Stock;

namespace Orizon.Distribuidora.Application.Tests.Stock;

public sealed class StockContractsTests
{
    [Fact]
    public void Indicators_CalculateBelowMinimum()
    {
        var result = StockLevelIndicators.Calculate(3, 5, true);
        Assert.Equal(-2, result.DifferenceToMinimum);
        Assert.True(result.IsBelowMinimum);
        Assert.False(result.IsOutOfStock);
    }

    [Fact]
    public void Indicators_NullMinimumIsNotCritical()
    {
        var result = StockLevelIndicators.Calculate(0, null, true);
        Assert.Null(result.DifferenceToMinimum);
        Assert.False(result.IsBelowMinimum);
        Assert.True(result.IsOutOfStock);
    }

    [Fact]
    public void Indicators_ProductWithoutControlIsExcluded()
    {
        var result = StockLevelIndicators.Calculate(0, 5, false);
        Assert.Null(result.DifferenceToMinimum);
        Assert.False(result.IsBelowMinimum);
        Assert.False(result.IsOutOfStock);
    }

    [Fact]
    public void OperationFailure_DoesNotExposeMovement()
    {
        var result = StockOperationResult.Failure("insufficient_stock", "Saldo insuficiente.");
        Assert.False(result.Succeeded);
        Assert.Null(result.MovementId);
        Assert.Null(result.ResultingQuantity);
    }
}
