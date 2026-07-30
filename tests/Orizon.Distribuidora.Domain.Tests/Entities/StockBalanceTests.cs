using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Tests.Entities;

public sealed class StockBalanceTests
{
    [Fact]
    public void Constructor_CreatesZeroBalance()
    {
        var balance = Create();
        Assert.Equal(0, balance.QuantityOnHand);
        Assert.Empty(balance.Movements);
    }

    [Fact]
    public void Entry_IncreasesBalanceAndCreatesOneCoherentMovement()
    {
        var balance = Create();
        var movement = balance.Apply(StockMovementType.PurchaseReceipt, 10.5m, "Compra");
        Assert.Equal(10.5m, balance.QuantityOnHand);
        Assert.Single(balance.Movements);
        Assert.Equal(0, movement.PreviousQuantity);
        Assert.Equal(10.5m, movement.ResultingQuantity);
        Assert.Equal(StockMovementDirection.Inbound, movement.Direction);
    }

    [Fact]
    public void Issue_DecreasesBalance()
    {
        var balance = Create();
        balance.Apply(StockMovementType.PurchaseReceipt, 10, "Entrada");
        var movement = balance.Apply(StockMovementType.SaleIssue, 4, "Venda");
        Assert.Equal(6, balance.QuantityOnHand);
        Assert.Equal(10, movement.PreviousQuantity);
        Assert.Equal(6, movement.ResultingQuantity);
        Assert.Equal(StockMovementDirection.Outbound, movement.Direction);
    }

    [Fact]
    public void Issue_AboveBalanceIsRejectedWithoutHistory()
    {
        var balance = Create();
        Assert.Throws<InvalidOperationException>(() => balance.Apply(StockMovementType.SaleIssue, 1, "Venda"));
        Assert.Empty(balance.Movements);
        Assert.Equal(0, balance.QuantityOnHand);
    }

    [Fact]
    public void PositiveAdjustment_IncreasesBalance()
    {
        var balance = Create();
        balance.Apply(StockMovementType.PositiveAdjustment, 2, "Contagem");
        Assert.Equal(2, balance.QuantityOnHand);
    }

    [Fact]
    public void NegativeAdjustment_DecreasesBalance()
    {
        var balance = Create();
        balance.Apply(StockMovementType.PurchaseReceipt, 5, "Entrada");
        balance.Apply(StockMovementType.NegativeAdjustment, 2, "Avaria");
        Assert.Equal(3, balance.QuantityOnHand);
    }

    [Fact]
    public void NegativeAdjustment_CannotMakeBalanceNegative()
    {
        var balance = Create();
        Assert.Throws<InvalidOperationException>(() =>
            balance.Apply(StockMovementType.NegativeAdjustment, 1, "Avaria"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Movement_RequiresPositiveQuantity(decimal quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create().Apply(StockMovementType.PurchaseReceipt, quantity, "Entrada"));
    }

    [Fact]
    public void Adjustment_RequiresReason()
    {
        Assert.Throws<ArgumentException>(() =>
            Create().Apply(StockMovementType.PositiveAdjustment, 1, " "));
    }

    [Fact]
    public void Movement_CalculatesTotalCost()
    {
        var movement = Create().Apply(StockMovementType.PurchaseReceipt, 3, "Compra", unitCost: 2.5m);
        Assert.Equal(7.5m, movement.TotalCost);
    }

    [Fact]
    public void Movement_PreservesSnapshotAfterLaterMovements()
    {
        var balance = Create();
        var first = balance.Apply(StockMovementType.PurchaseReceipt, 3, "Primeira");
        balance.Apply(StockMovementType.PurchaseReceipt, 2, "Segunda");
        Assert.Equal(0, first.PreviousQuantity);
        Assert.Equal(3, first.ResultingQuantity);
        Assert.Equal("Primeira", first.Reason);
    }

    [Fact]
    public void Mutation_RenewsConcurrencyToken()
    {
        var balance = Create();
        var token = balance.ConcurrencyToken;
        balance.Apply(StockMovementType.PurchaseReceipt, 1, "Entrada");
        Assert.NotEqual(token, balance.ConcurrencyToken);
    }

    [Fact]
    public void ImportacaoEstoqueInicial_CreatesAuditableIdempotentInboundMovement()
    {
        var balance = Create();
        var historyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var movement = balance.Apply(StockMovementType.InitialBalance, 8, "ImportaÃ§Ã£o â€” estoque inicial",
            internalLocationId: Guid.NewGuid(), referenceType: "ImportacaoHistorico",
            referenceId: historyId.ToString(), operationKey: $"importacao:{historyId}:linha:2", createdBy: userId);

        Assert.Equal(StockMovementDirection.Inbound, movement.Direction);
        Assert.Equal("ImportacaoHistorico", movement.ReferenceType);
        Assert.Equal(historyId.ToString(), movement.ReferenceId);
        Assert.Equal($"importacao:{historyId}:linha:2", movement.OperationKey);
        Assert.Equal(userId, movement.CreatedBy);
        Assert.Equal(8, balance.QuantityOnHand);
    }

    [Theory]
    [InlineData(StockMovementType.TransferIn, StockMovementDirection.Inbound)]
    [InlineData(StockMovementType.TransferOut, StockMovementDirection.Outbound)]
    [InlineData(StockMovementType.ReturnIn, StockMovementDirection.Inbound)]
    [InlineData(StockMovementType.ReturnOut, StockMovementDirection.Outbound)]
    public void PreparedTypes_HaveCoherentDirection(StockMovementType type, StockMovementDirection direction)
    {
        var balance = Create();
        if (direction == StockMovementDirection.Outbound)
            balance.Apply(StockMovementType.PurchaseReceipt, 2, "Preparação");
        Assert.Equal(direction, balance.Apply(type, 1, "Referência").Direction);
    }

    [Fact]
    public void Constructor_RejectsInvalidIdentifiers()
    {
        Assert.Throws<ArgumentException>(() => new StockBalance(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new StockBalance(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()));
        Assert.Throws<ArgumentException>(() => new StockBalance(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));
    }

    private static StockBalance Create() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
}
