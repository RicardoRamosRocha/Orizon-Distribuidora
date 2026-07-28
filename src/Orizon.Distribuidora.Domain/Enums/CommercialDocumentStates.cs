namespace Orizon.Distribuidora.Domain.Enums;

public enum QuoteStatus
{
    Draft,
    Sent,
    Approved,
    Rejected,
    Expired,
    Converted,
    Cancelled
}

public enum SaleStatus
{
    Draft,
    Confirmed,
    AwaitingPayment,
    Paid,
    PartiallyPaid,
    InFulfillment,
    Completed,
    Cancelled
}

public enum PaymentStatus
{
    Pending,
    PartiallyPaid,
    Paid,
    Cancelled
}

public enum FiscalDocumentStatus
{
    NotRequested,
    Pending,
    Processing,
    Authorized,
    Rejected,
    Cancelled
}
