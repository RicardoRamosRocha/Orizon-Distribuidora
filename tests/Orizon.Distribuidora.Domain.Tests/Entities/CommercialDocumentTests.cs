using Orizon.Distribuidora.Domain.Entities;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Tests.Entities;

public sealed class CommercialDocumentTests
{
    private static readonly Guid CompanyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static QuoteItem Item(decimal quantity = 2, decimal price = 10, decimal discount = 1,
        bool own = true, bool controlsStock = true) =>
        new(CompanyId, Guid.NewGuid(), "P001", "Produto snapshot", "UN", quantity, price, discount,
            own, controlsStock, Guid.NewGuid());
    private static Quote Quote(DateOnly? validity = null)
    {
        var q = new Quote(CompanyId, 1, Guid.NewGuid(), "Cliente snapshot", "123", Guid.NewGuid(),
            DateTimeOffset.UtcNow, validity ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), null, null, null);
        q.ReplaceDraft(null, null, q.ValidUntil, 2, 3, 1, [Item()]);
        return q;
    }

    [Fact]
    public void Item_calculates_total_with_commercial_rounding()
    {
        var item = new QuoteItem(CompanyId, Guid.NewGuid(), "A", "Item", "UN", 3, 10.015m, .01m, true, true, null);
        Assert.Equal(30.05m, item.Total);
    }

    [Fact]
    public void Item_recalculates_total_when_quantity_changes_from_one_to_two()
    {
        var one = Item(quantity: 1, price: 10, discount: 0);
        var two = Item(quantity: 2, price: 10, discount: 0);

        Assert.Equal(10, one.Total);
        Assert.Equal(20, two.Total);
    }

    [Fact]
    public void Item_preserves_fractional_quantity_supported_by_domain()
    {
        var item = Item(quantity: 1.5m, price: 10, discount: 0);

        Assert.Equal(1.5m, item.Quantity);
        Assert.Equal(15, item.Total);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Item_rejects_non_positive_quantity(decimal quantity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Item(quantity));

    [Fact]
    public void Quote_recalculates_totals_on_server()
    {
        var quote = Quote();
        Assert.Equal(19, quote.Subtotal);
        Assert.Equal(21, quote.Total);
    }

    [Fact]
    public void Quote_supports_valid_transitions_and_rejects_invalid_ones()
    {
        var quote = Quote(); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        quote.MarkSent(today); quote.Approve(today); quote.MarkConverted(Guid.NewGuid());
        Assert.Equal(QuoteStatus.Converted, quote.Status);
        Assert.Throws<InvalidOperationException>(() => quote.Cancel());
    }

    [Fact]
    public void Quote_can_be_rejected_only_after_sent()
    {
        var quote = Quote();
        Assert.Throws<InvalidOperationException>(() => quote.Reject());
        quote.MarkSent(DateOnly.FromDateTime(DateTime.UtcNow)); quote.Reject();
        Assert.Equal(QuoteStatus.Rejected, quote.Status);
    }

    [Fact]
    public void Expiration_is_detected_without_affecting_final_states()
    {
        var quote = Quote(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)));
        Assert.True(quote.Expire(DateOnly.FromDateTime(DateTime.UtcNow)));
        Assert.Equal(QuoteStatus.Expired, quote.Status);
    }

    [Fact]
    public void Conversion_is_idempotent_for_same_sale()
    {
        var quote = Quote(); var today = DateOnly.FromDateTime(DateTime.UtcNow);
        quote.MarkSent(today); quote.Approve(today); var saleId = Guid.NewGuid();
        quote.MarkConverted(saleId); quote.MarkConverted(saleId);
        Assert.Equal(saleId, quote.SaleId);
    }

    [Fact]
    public void Sale_preserves_snapshots_and_fiscal_state_is_independent()
    {
        var item = new SaleItem(CompanyId, Guid.NewGuid(), "OLD", "Descrição histórica", "CX",
            2, 4, 0, 8, false, false, null);
        var sale = new Sale(CompanyId, 1, Guid.NewGuid(), "Cliente histórico", null, Guid.NewGuid(),
            Guid.NewGuid(), DateTimeOffset.UtcNow, 8, 0, 0, 0, null, null, [item]);
        sale.Confirm(DateTimeOffset.UtcNow);
        Assert.Equal("Descrição histórica", sale.Items.Single().Description);
        Assert.Equal(SaleStatus.Confirmed, sale.Status);
        Assert.Equal(FiscalDocumentStatus.NotRequested, sale.FiscalStatus);
    }

    [Fact]
    public void Confirmed_sale_cannot_be_cancelled_without_return()
    {
        var item = new SaleItem(CompanyId, Guid.NewGuid(), "A", "Item", "UN", 1, 1, 0, 1, true, true, Guid.NewGuid());
        var sale = new Sale(CompanyId, 1, Guid.NewGuid(), "Cliente", null, null, null,
            DateTimeOffset.UtcNow, 1, 0, 0, 0, null, null, [item]);
        sale.Confirm(DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => sale.Cancel(DateTimeOffset.UtcNow));
    }
}
