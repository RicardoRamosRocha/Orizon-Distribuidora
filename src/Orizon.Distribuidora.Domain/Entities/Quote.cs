using Orizon.Distribuidora.Domain.Common;
using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Domain.Entities;

public sealed class Quote : CompanyOwnedAuditableEntity
{
    private readonly List<QuoteItem> _items = [];
    private Quote() { }

    public Quote(Guid companyId, long number, Guid customerId, string customerName,
        string? customerDocument, Guid? sellerUserId, DateTimeOffset issuedAt,
        DateOnly validUntil, Guid? priceTableId, string? notes, string? deliveryAddress)
        : base(companyId)
    {
        if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number));
        if (customerId == Guid.Empty) throw new ArgumentException("O cliente é obrigatório.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(customerName)) throw new ArgumentException("O nome do cliente é obrigatório.", nameof(customerName));
        Number = number;
        CustomerId = customerId;
        CustomerName = customerName.Trim();
        CustomerDocument = Normalize(customerDocument);
        SellerUserId = sellerUserId;
        IssuedAt = issuedAt;
        ValidUntil = validUntil;
        PriceTableId = priceTableId;
        Notes = Normalize(notes);
        DeliveryAddress = Normalize(deliveryAddress);
        Status = QuoteStatus.Draft;
        ConcurrencyToken = Guid.NewGuid();
    }

    public long Number { get; private set; }
    public string DisplayNumber => $"ORC-{Number:000000}";
    public Guid CustomerId { get; private set; }
    public CommercialPartner? Customer { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string? CustomerDocument { get; private set; }
    public Guid? SellerUserId { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateOnly ValidUntil { get; private set; }
    public QuoteStatus Status { get; private set; }
    public Guid? PriceTableId { get; private set; }
    public PriceTable? PriceTable { get; private set; }
    public string? Notes { get; private set; }
    public string? DeliveryAddress { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Freight { get; private set; }
    public decimal AdditionalCharges { get; private set; }
    public decimal Total { get; private set; }
    public Guid? SaleId { get; private set; }
    public Guid ConcurrencyToken { get; private set; }
    public ICollection<QuoteItem> Items => _items;

    public void ReplaceDraft(string? notes, string? deliveryAddress, DateOnly validUntil,
        decimal discount, decimal freight, decimal additionalCharges, IEnumerable<QuoteItem> items)
    {
        EnsureDraft();
        if (discount < 0 || freight < 0 || additionalCharges < 0)
            throw new ArgumentOutOfRangeException(nameof(discount), "Os valores do documento não podem ser negativos.");
        var materialized = items.ToList();
        if (materialized.Count == 0) throw new InvalidOperationException("Inclua ao menos um item.");
        _items.Clear();
        _items.AddRange(materialized);
        Notes = Normalize(notes);
        DeliveryAddress = Normalize(deliveryAddress);
        ValidUntil = validUntil;
        Subtotal = Money(materialized.Sum(x => x.Total));
        Discount = Money(discount);
        Freight = Money(freight);
        AdditionalCharges = Money(additionalCharges);
        Total = Money(Subtotal - Discount + Freight + AdditionalCharges);
        if (Total < 0) throw new InvalidOperationException("O total do orçamento não pode ser negativo.");
        Touch();
    }

    public void ChangeCustomer(Guid customerId, string customerName, string? customerDocument)
    {
        EnsureDraft();
        if (customerId == Guid.Empty || string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("O cliente é obrigatório.");
        CustomerId = customerId;
        CustomerName = customerName.Trim();
        CustomerDocument = Normalize(customerDocument);
        Touch();
    }

    public void MarkSent(DateOnly today)
    {
        EnsureNotExpired(today);
        if (Status != QuoteStatus.Draft) throw InvalidTransition();
        Status = QuoteStatus.Sent;
        Touch();
    }

    public void Approve(DateOnly today)
    {
        EnsureNotExpired(today);
        if (Status != QuoteStatus.Sent) throw InvalidTransition();
        Status = QuoteStatus.Approved;
        Touch();
    }

    public void Reject()
    {
        if (Status != QuoteStatus.Sent) throw InvalidTransition();
        Status = QuoteStatus.Rejected;
        Touch();
    }

    public void Cancel()
    {
        if (Status is QuoteStatus.Converted or QuoteStatus.Cancelled) throw InvalidTransition();
        Status = QuoteStatus.Cancelled;
        Touch();
    }

    public bool Expire(DateOnly today)
    {
        if (ValidUntil >= today || Status is not (QuoteStatus.Draft or QuoteStatus.Sent)) return false;
        Status = QuoteStatus.Expired;
        Touch();
        return true;
    }

    public void MarkConverted(Guid saleId)
    {
        if (SaleId == saleId && Status == QuoteStatus.Converted) return;
        if (Status != QuoteStatus.Approved || SaleId.HasValue) throw InvalidTransition();
        SaleId = saleId;
        Status = QuoteStatus.Converted;
        Touch();
    }

    private void EnsureDraft()
    {
        if (Status != QuoteStatus.Draft) throw InvalidTransition();
    }
    private void EnsureNotExpired(DateOnly today)
    {
        if (ValidUntil < today) { Expire(today); throw new InvalidOperationException("O orçamento está vencido."); }
    }
    private void Touch() => ConcurrencyToken = Guid.NewGuid();
    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static InvalidOperationException InvalidTransition() => new("A transição de situação solicitada não é permitida.");
}

public sealed class QuoteItem : Entity
{
    private QuoteItem() { }
    public QuoteItem(Guid companyId, Guid productId, string productCode, string description,
        string unit, decimal quantity, decimal unitPrice, decimal discount,
        bool isOwnProduct, bool controlsStock, Guid? warehouseId)
    {
        if (companyId == Guid.Empty || productId == Guid.Empty) throw new ArgumentException("Empresa e produto são obrigatórios.");
        if (string.IsNullOrWhiteSpace(productCode) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("O snapshot comercial do produto é obrigatório.");
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "A quantidade deve ser maior que zero.");
        if (unitPrice < 0 || discount < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "Preço e desconto não podem ser negativos.");
        CompanyId = companyId; ProductId = productId; ProductCode = productCode.Trim();
        Description = description.Trim(); Unit = unit.Trim(); Quantity = quantity;
        UnitPrice = Money(unitPrice); Discount = Money(discount);
        var gross = Money(quantity * UnitPrice);
        if (Discount > gross) throw new InvalidOperationException("O desconto do item não pode superar seu valor bruto.");
        Total = Money(gross - Discount); IsOwnProduct = isOwnProduct; ControlsStock = controlsStock;
        WarehouseId = warehouseId;
    }
    public Guid CompanyId { get; private set; }
    public Guid QuoteId { get; private set; }
    public Quote? Quote { get; private set; }
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
    private static decimal Money(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
