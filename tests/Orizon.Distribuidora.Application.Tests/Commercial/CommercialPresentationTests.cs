using System.Globalization;
using Orizon.Distribuidora.Domain.Enums;
using Orizon.Distribuidora.Web.Areas.Admin.Models.Commercial;

namespace Orizon.Distribuidora.Application.Tests.Commercial;

public sealed class CommercialPresentationTests
{
    [Theory]
    [InlineData(QuoteStatus.Draft, "Rascunho")]
    [InlineData(QuoteStatus.Sent, "Enviado")]
    [InlineData(QuoteStatus.Approved, "Aprovado")]
    [InlineData(QuoteStatus.Rejected, "Recusado")]
    [InlineData(QuoteStatus.Expired, "Vencido")]
    [InlineData(QuoteStatus.Converted, "Convertido")]
    [InlineData(QuoteStatus.Cancelled, "Cancelado")]
    public void Quote_states_are_presented_in_portuguese(QuoteStatus status, string expected) =>
        Assert.Equal(expected, CommercialPresentation.Label(status));

    [Theory]
    [InlineData(SaleStatus.Draft, "Rascunho")]
    [InlineData(SaleStatus.Confirmed, "Confirmada")]
    [InlineData(SaleStatus.AwaitingPayment, "Aguardando pagamento")]
    [InlineData(SaleStatus.Paid, "Paga")]
    [InlineData(SaleStatus.PartiallyPaid, "Parcialmente paga")]
    [InlineData(SaleStatus.InFulfillment, "Em atendimento")]
    [InlineData(SaleStatus.Completed, "Concluída")]
    [InlineData(SaleStatus.Cancelled, "Cancelada")]
    public void Sale_states_are_presented_in_portuguese(SaleStatus status, string expected) =>
        Assert.Equal(expected, CommercialPresentation.Label(status));

    [Theory]
    [InlineData(PaymentStatus.Pending, "Pendente")]
    [InlineData(PaymentStatus.PartiallyPaid, "Parcial")]
    [InlineData(PaymentStatus.Paid, "Pago")]
    [InlineData(PaymentStatus.Cancelled, "Cancelado")]
    public void Payment_states_are_presented_in_portuguese(PaymentStatus status, string expected) =>
        Assert.Equal(expected, CommercialPresentation.Label(status));

    [Fact]
    public void Money_always_uses_pt_br_formatting()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Assert.Equal("R$ 1.234,56", CommercialPresentation.Money(1234.56m).Replace('\u00A0', ' '));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
