using OrderHub.Application.Features.Inventory.Approvals.Contracts;
using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Inventory.Approvals;

public static class ApprovalPresentation
{
    public static string TypeLabel(InventoryApprovalRequestType type) => type switch
    {
        InventoryApprovalRequestType.AllocationEdit => "تعديل طلب",
        InventoryApprovalRequestType.IssueOrderCancel => "إلغاء أمر صرف",
        InventoryApprovalRequestType.MovementReversal => "عكس حركة",
        InventoryApprovalRequestType.DamagedDisposal => "إتلاف تالف",
        _ => "طلب اعتماد"
    };

    public static string TypeTag(InventoryApprovalRequestType type) => type switch
    {
        InventoryApprovalRequestType.AllocationEdit => "border-sky-200 bg-sky-50 text-sky-700",
        InventoryApprovalRequestType.IssueOrderCancel => "border-orange-200 bg-orange-50 text-orange-700",
        InventoryApprovalRequestType.MovementReversal => "border-violet-200 bg-violet-50 text-violet-700",
        InventoryApprovalRequestType.DamagedDisposal => "border-rose-200 bg-rose-50 text-rose-700",
        _ => "border-slate-200 bg-slate-50 text-slate-600"
    };

    public static string StatusLabel(InventoryApprovalStatus status) => status switch
    {
        InventoryApprovalStatus.Pending => "قيد الاعتماد",
        InventoryApprovalStatus.Approved => "معتمد",
        InventoryApprovalStatus.Rejected => "مرفوض",
        InventoryApprovalStatus.Stale => "متقادم",
        _ => "غير محدد"
    };

    public static string StatusTag(InventoryApprovalStatus status) => status switch
    {
        InventoryApprovalStatus.Pending => "border-amber-200 bg-amber-50 text-amber-800",
        InventoryApprovalStatus.Approved => "border-emerald-200 bg-emerald-50 text-emerald-700",
        InventoryApprovalStatus.Rejected => "border-rose-200 bg-rose-50 text-rose-700",
        InventoryApprovalStatus.Stale => "border-slate-200 bg-slate-100 text-slate-600",
        _ => "border-slate-200 bg-white text-slate-600"
    };

    public static string TargetLabel(InventoryApprovalProjectionRow row) => row.RequestType switch
    {
        InventoryApprovalRequestType.AllocationEdit => row.OrderNumber ?? "طلب غير معروف",
        InventoryApprovalRequestType.IssueOrderCancel =>
            $"أمر صرف {IssueOrderNumberLabel(row.IssueOrderNumber, row.IssueOrderSequenceYear, row.IssueOrderSequenceMonth)} — {row.IssueOrderOrderNumber}",
        InventoryApprovalRequestType.MovementReversal =>
            $"{row.MovementProductName ?? "منتج"} · {row.MovementWarehouseName} · {row.MovementQuantity.ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        InventoryApprovalRequestType.DamagedDisposal =>
            $"{row.DisposalProductName ?? "منتج"} · {row.DisposalWarehouseName} · {(row.DisposalQuantity ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture)}",
        _ => "—"
    };

    public static string IssueOrderNumberLabel(long? number, int? year, int? month)
    {
        if (number is not long value)
            return "—";
        if (year is int y && month is int m)
            return $"{value.ToString("D4", System.Globalization.CultureInfo.InvariantCulture)}/{m.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)}/{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
