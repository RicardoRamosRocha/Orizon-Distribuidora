namespace Orizon.Distribuidora.Domain.Enums;

public enum StockMovementType
{
    InitialBalance = 1,
    PurchaseReceipt,
    SaleIssue,
    PositiveAdjustment,
    NegativeAdjustment,
    TransferIn,
    TransferOut,
    ReturnIn,
    ReturnOut,
    InventoryCorrection
}
