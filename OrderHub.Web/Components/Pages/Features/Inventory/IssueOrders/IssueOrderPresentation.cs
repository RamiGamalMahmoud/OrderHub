using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Inventory.IssueOrders;

internal static class IssueOrderPresentation
{
    public static string StatusLabel(WorkRecordStatus status) => status switch
    {
        WorkRecordStatus.Pending => "بانتظار الصرف",
        WorkRecordStatus.PartiallyExecuted => "صرف جزئي",
        WorkRecordStatus.CancelRequested => "طلب إلغاء معلّق",
        WorkRecordStatus.Completed => "مكتمل",
        WorkRecordStatus.Cancelled => "ملغى",
        WorkRecordStatus.Failed => "متعذر",
        _ => status.ToString()
    };

    public static string StatusTag(WorkRecordStatus status) => status switch
    {
        WorkRecordStatus.Pending => "border-amber-200 bg-amber-50 text-amber-700",
        WorkRecordStatus.PartiallyExecuted => "border-sky-200 bg-sky-50 text-sky-700",
        WorkRecordStatus.CancelRequested => "border-orange-200 bg-orange-50 text-orange-700",
        WorkRecordStatus.Completed => "border-emerald-200 bg-emerald-50 text-emerald-700",
        WorkRecordStatus.Cancelled => "border-slate-200 bg-slate-100 text-slate-600",
        WorkRecordStatus.Failed => "border-rose-200 bg-rose-50 text-rose-700",
        _ => "border-slate-200 bg-slate-100 text-slate-600"
    };

    public static bool IsActive(WorkRecordStatus status) =>
        status is WorkRecordStatus.Pending or WorkRecordStatus.PartiallyExecuted;

    public static string NumberLabel(long number, int year, int month) =>
        $"{number:D5}/{month:D2}/{year}";
}
