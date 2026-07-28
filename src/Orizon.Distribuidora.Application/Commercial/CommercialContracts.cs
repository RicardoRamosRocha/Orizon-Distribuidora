using Orizon.Distribuidora.Domain.Enums;

namespace Orizon.Distribuidora.Application.Commercial;

public sealed record CommercialItemInput(Guid ProductId, decimal Quantity, decimal? UnitPrice = null,
    decimal Discount = 0, Guid? WarehouseId = null);
public sealed record SaveQuoteRequest(Guid CustomerId, DateOnly ValidUntil, Guid? PriceTableId,
    decimal Discount, decimal Freight, decimal AdditionalCharges, string? Notes,
    string? DeliveryAddress, IReadOnlyList<CommercialItemInput> Items, Guid? ConcurrencyToken = null);
public sealed record CommercialResult(bool Succeeded, string? ErrorCode = null, string? ErrorMessage = null,
    Guid? DocumentId = null)
{
    public static CommercialResult Success(Guid id) => new(true, DocumentId: id);
    public static CommercialResult Failure(string code, string message) => new(false, code, message);
}
public sealed record CommercialOption(Guid Id, string Label);
public sealed record ProductSearchResult(Guid Id, string Code, string Description, string Unit,
    decimal UnitPrice, bool ControlsStock, bool IsOwnProduct, Guid? DefaultWarehouseId);
public sealed record CommercialOptions(IReadOnlyList<CommercialOption> Customers,
    IReadOnlyList<CommercialOption> PriceTables, IReadOnlyList<CommercialOption> Warehouses);

public sealed record QuoteFilter(string? Search = null, Guid? CustomerId = null, QuoteStatus? Status = null,
    DateOnly? From = null, DateOnly? To = null, DateOnly? ValidUntil = null, Guid? SellerUserId = null,
    string SortBy = "issued", string SortDirection = "desc", int Page = 1, int PageSize = 25);
public sealed record SaleFilter(string? Search = null, Guid? CustomerId = null, SaleStatus? Status = null,
    PaymentStatus? PaymentStatus = null, FiscalDocumentStatus? FiscalStatus = null,
    DateOnly? From = null, DateOnly? To = null, string SortBy = "issued",
    string SortDirection = "desc", int Page = 1, int PageSize = 25);
public sealed record CommercialPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
public sealed record QuoteListItem(Guid Id, long Number, string CustomerName, DateTimeOffset IssuedAt,
    DateOnly ValidUntil, QuoteStatus Status, decimal Total, Guid? SellerUserId, Guid? SaleId);
public sealed record SaleListItem(Guid Id, long Number, string CustomerName, DateTimeOffset IssuedAt,
    SaleStatus Status, PaymentStatus PaymentStatus, FiscalDocumentStatus FiscalStatus,
    decimal Total, Guid? QuoteId);
public sealed record QuoteSummary(int Open, int AwaitingResponse, int Approved, int Expired,
    int Converted, decimal OpenValue, decimal ConversionRate);
public sealed record SaleSummary(int Total, int Confirmed, int AwaitingPayment, int Completed,
    int Cancelled, decimal TotalValue);
public sealed record CommercialItemDetail(Guid ProductId, string ProductCode, string Description,
    string Unit, decimal Quantity, decimal UnitPrice, decimal Discount, decimal Total,
    bool ControlsStock, bool IsOwnProduct, Guid? WarehouseId);
public sealed record QuoteDetail(Guid Id, long Number, Guid CustomerId, string CustomerName,
    string? CustomerDocument, DateTimeOffset IssuedAt, DateOnly ValidUntil, QuoteStatus Status,
    Guid? PriceTableId, string? Notes, string? DeliveryAddress, decimal Subtotal, decimal Discount,
    decimal Freight, decimal AdditionalCharges, decimal Total, Guid? SaleId, Guid ConcurrencyToken,
    IReadOnlyList<CommercialItemDetail> Items);
public sealed record SaleDetail(Guid Id, long Number, Guid CustomerId, string CustomerName,
    string? CustomerDocument, DateTimeOffset IssuedAt, DateTimeOffset? ConfirmedAt,
    SaleStatus Status, PaymentStatus PaymentStatus, FiscalDocumentStatus FiscalStatus,
    decimal Subtotal, decimal Discount, decimal Freight, decimal AdditionalCharges, decimal Total,
    string? Notes, string? DeliveryAddress, Guid? QuoteId, Guid ConcurrencyToken,
    IReadOnlyList<CommercialItemDetail> Items);
public sealed record CompanyDocumentHeader(string LegalName, string TradeName, string Document);

public interface ICommercialService
{
    Task<(CommercialPage<QuoteListItem> Page, QuoteSummary Summary)> ListQuotesAsync(Guid companyId, QuoteFilter filter, CancellationToken cancellationToken = default);
    Task<(CommercialPage<SaleListItem> Page, SaleSummary Summary)> ListSalesAsync(Guid companyId, SaleFilter filter, CancellationToken cancellationToken = default);
    Task<CommercialOptions> GetOptionsAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductSearchResult>> SearchProductsAsync(Guid companyId, string? search, Guid? priceTableId, CancellationToken cancellationToken = default);
    Task<QuoteDetail?> GetQuoteAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<SaleDetail?> GetSaleAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default);
    Task<CompanyDocumentHeader?> GetCompanyHeaderAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<CommercialResult> CreateQuoteAsync(Guid companyId, Guid? userId, SaveQuoteRequest request, bool markSent, CancellationToken cancellationToken = default);
    Task<CommercialResult> UpdateQuoteAsync(Guid companyId, Guid? userId, Guid id, SaveQuoteRequest request, bool markSent, CancellationToken cancellationToken = default);
    Task<CommercialResult> ChangeQuoteStatusAsync(Guid companyId, Guid? userId, Guid id, QuoteStatus target, CancellationToken cancellationToken = default);
    Task<CommercialResult> ConvertQuoteAsync(Guid companyId, Guid? userId, Guid id, CancellationToken cancellationToken = default);
    Task<CommercialResult> ConfirmSaleAsync(Guid companyId, Guid? userId, Guid id, CancellationToken cancellationToken = default);
    Task<CommercialResult> CancelSaleAsync(Guid companyId, Guid? userId, Guid id, CancellationToken cancellationToken = default);
}

public sealed record FiscalDocumentRequest(Guid CompanyId, Guid SaleId, string DocumentNumber);
public sealed record FiscalDocumentResult(bool Succeeded, FiscalDocumentStatus Status, string Message,
    string? ExternalId = null, string? AccessKey = null);
public interface IFiscalDocumentService
{
    Task<FiscalDocumentResult> RequestAsync(FiscalDocumentRequest request, CancellationToken cancellationToken = default);
}
