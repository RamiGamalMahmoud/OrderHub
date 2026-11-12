namespace OrderHub.Domain.Enums;

public enum InventoryMovementType
{
    OpeningStock = 1,
    StockIn = 2,
    IssueOut = 3,
    ReturnIn = 4,
    ReturnDamagedIn = 5,
    DamagedDisposal = 6,
    Adjustment = 7,
    Reversal = 8,
    DamagedAdjustment = 9
}
