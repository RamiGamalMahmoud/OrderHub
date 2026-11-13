using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Inventory.Counts;

internal static class CountPresentation
{
    public static string StatusLabel(InventoryCountStatus status) => status switch
    {
        InventoryCountStatus.Open => "قيد العدّ",
        InventoryCountStatus.UnderReview => "بانتظار الاعتماد",
        InventoryCountStatus.Approved => "معتمد",
        InventoryCountStatus.Cancelled => "ملغى",
        _ => status.ToString()
    };

    public static string StatusTag(InventoryCountStatus status) => status switch
    {
        InventoryCountStatus.Open => "border-sky-200 bg-sky-50 text-sky-700",
        InventoryCountStatus.UnderReview => "border-amber-200 bg-amber-50 text-amber-700",
        InventoryCountStatus.Approved => "border-emerald-200 bg-emerald-50 text-emerald-700",
        InventoryCountStatus.Cancelled => "border-slate-200 bg-slate-100 text-slate-600",
        _ => "border-slate-200 bg-slate-100 text-slate-600"
    };

    public static string ScopeLabel(InventoryCountScope scope) => scope switch
    {
        InventoryCountScope.Full => "جرد كامل",
        InventoryCountScope.Products => "أصناف محددة",
        _ => scope.ToString()
    };

    public static string DeltaLabel(int? delta) => delta switch
    {
        null => "—",
        > 0 => $"+{delta.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        _ => delta.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };

    public static string DeltaTag(int? delta) => delta switch
    {
        null => "border-slate-200 bg-slate-50 text-slate-500",
        > 0 => "border-emerald-200 bg-emerald-50 text-emerald-700",
        < 0 => "border-rose-200 bg-rose-50 text-rose-700",
        _ => "border-slate-200 bg-slate-100 text-slate-600"
    };
}
