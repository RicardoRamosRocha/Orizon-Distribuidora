using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class Sale : CompanyOwnedAuditableEntity
{
    private readonly List<SaleItem> _items = [];
    private Sale() { }
    public Sale(Guid companyId, long number, Guid customerId, string customerName,
        string? customerDocument, Guid? quoteId, Guid? sellerUserId, DateTimeOffset issuedAt,
        decimal subtotal, decimal discount, decimal freight, decimal additionalCharges,
        string? notes, string? deliveryAddress, IEnumerable<SaleItem> items) : base(companyId)
    {
        var materialized = items.ToList();
        if (number <= 0 || customerId == Guid.Empty || materialized.Count == 0) throw new ArgumentException("Os dados da venda são obrigatórios.");
        Number = number; CustomerId = customerId; CustomerName = customerName.Trim();
        CustomerDocument = Normalize(customerDocument); QuoteId = quoteId; SellerUserId = sellerUserId;
        IssuedAt = issuedAt; Subtotal = Money(subtotal); Discount = Money(discount); Freight = Money(freight);
        AdditionalCharges = Money(additionalCharges); Total = Money(Subtotal - Discount + Freight + AdditionalCharges);
        if (Total < 0) throw new InvalidOperationException("O total da venda não pode ser negativo.");
        Notes = Normalize(notes); DeliveryAddress = Normalize(deliveryAddress); _items.AddRange(materialized);
        Status = SaleStatus.Draft; PaymentStatus = PaymentStatus.Pending; FiscalStatus = FiscalDocumentStatus.NotRequested;
        ConcurrencyToken = Guid.NewGuid();
    }
    public long Number { get; private set; }
    public string DisplayNumber => $"VEN-{Number:000000}";
    public Guid CustomerId { get; private set; }
    public CommercialPartner? Customer { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string? CustomerDocument { get; private set; }
    public Guid? QuoteId { get; private set; }
    public Quote? Quote { get; private set; }
    public Guid? SellerUserId { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public SaleStatus Status { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
    public FiscalDocumentStatus FiscalStatus { get; private set; }
    public string? FiscalExternalId { get; private set; }
    public string? FiscalAccessKey { get; private set; }
    public string? FiscalMessage { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Freight { get; private set; }
    public decimal AdditionalCharges { get; private set; }
    public decimal Total { get; private set; }
    public string? Notes { get; private set; }
    public string? DeliveryAddress { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public ICollection<SaleItem> Items => _items;

    public void Confirm(DateTimeOffset now)
    {
        if (Status == SaleStatus.Confirmed) return;
        if (Status != SaleStatus.Draft) throw new InvalidOperationException("Somente vendas em rascunho podem ser confirmadas.");
        Status = SaleStatus.Confirmed; ConfirmedAt = now; ConcurrencyToken = Guid.NewGuid();
    }
    public void Cancel(DateTimeOffset now)
    {
        if (Status == SaleStatus.Cancelled) return;
        if (Status != SaleStatus.Draft) throw new InvalidOperationException("Após a confirmação, a venda não pode ser cancelada sem uma devolução de estoque.");
        Status = SaleStatus.Cancelled; PaymentStatus = PaymentStatus.Cancelled; CancelledAt = now; ConcurrencyToken = Guid.NewGuid();
    }
    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class SaleItem : Entity
{
    private SaleItem() { }
    public SaleItem(Guid companyId, Guid productId, string productCode, string description,
        string unit, decimal quantity, decimal unitPrice, decimal discount, decimal total,
        bool isOwnProduct, bool controlsStock, Guid? warehouseId)
    {
        if (companyId == Guid.Empty || productId == Guid.Empty || quantity <= 0) throw new ArgumentException("Item de venda inválido.");
        if (unitPrice < 0 || discount < 0 || total < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
        CompanyId = companyId; ProductId = productId; ProductCode = productCode.Trim(); Description = description.Trim();
        Unit = unit.Trim(); Quantity = quantity; UnitPrice = unitPrice; Discount = discount; Total = total;
        IsOwnProduct = isOwnProduct; ControlsStock = controlsStock; WarehouseId = warehouseId;
    }
    public Guid CompanyId { get; private set; }
    public Guid SaleId { get; private set; }
    public Sale? Sale { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Total { get; private set; }
    public bool IsOwnProduct { get; private set; }
    public bool ControlsStock { get; private set; }
    public Guid? WarehouseId { get; private set; }
}
