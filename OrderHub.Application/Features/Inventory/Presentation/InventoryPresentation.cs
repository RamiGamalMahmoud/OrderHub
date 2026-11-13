using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Inventory.Presentation;

public static class InventoryPresentation
{
    public static string StockStatusLabel(StockStatusFilter status) => status switch
    {
        StockStatusFilter.Available => "متاح",
        StockStatusFilter.PartiallyReserved => "محجوز جزئياً",
        StockStatusFilter.FullyReserved => "محجوز بالكامل",
        StockStatusFilter.OutOfStock => "نفد",
        _ => "الكل"
    };

    public static StockStatusFilter StockStatusOf(int stockOnHand, int reservedOnHand)
    {
        if (stockOnHand == 0)
            return StockStatusFilter.OutOfStock;
        if (stockOnHand - reservedOnHand == 0)
            return StockStatusFilter.FullyReserved;
        return reservedOnHand > 0 ? StockStatusFilter.PartiallyReserved : StockStatusFilter.Available;
    }

    public static string MovementTypeLabel(InventoryMovementType type) => type switch
    {
        InventoryMovementType.OpeningStock => "رصيد افتتاحي",
        InventoryMovementType.StockIn => "توريد",
        InventoryMovementType.IssueOut => "صرف",
        InventoryMovementType.ReturnIn => "مرتجع سليم",
        InventoryMovementType.ReturnDamagedIn => "مرتجع تالف",
        InventoryMovementType.DamagedDisposal => "إتلاف تالف",
        InventoryMovementType.Adjustment => "تسوية جرد",
        InventoryMovementType.Reversal => "عكس حركة",
        InventoryMovementType.DamagedAdjustment => "تسوية تالف",
        _ => type.ToString()
    };

    public static string MovementDirectionLabel(InventoryDirection direction) => direction switch
    {
        InventoryDirection.In => "داخل",
        InventoryDirection.Out => "خارج",
        _ => direction.ToString()
    };

    public static string MovementStatusLabel(InventoryMovementStatus status) => status switch
    {
        InventoryMovementStatus.Active => "سارية",
        InventoryMovementStatus.Reversed => "معكوسة",
        _ => status.ToString()
    };

    public static string IssueOrderNumberLabel(long number, int year, int month) =>
        $"{number:D5}/{month:D2}/{year}";

    public static string BuildLocation(string cityName, string street)
    {
        if (string.IsNullOrWhiteSpace(cityName) && string.IsNullOrWhiteSpace(street))
            return null;

        return string.IsNullOrWhiteSpace(street) ? cityName : $"{cityName} - {street}";
    }
}
