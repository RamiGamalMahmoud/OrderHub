using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum InventoryApprovalRequestType
{
    [Description("تعديل تخصيصات طلب")]
    AllocationEdit = 1,

    [Description("إلغاء أمر صرف")]
    IssueOrderCancel = 2,

    [Description("عكس حركة مخزون")]
    MovementReversal = 3,

    [Description("إتلاف كمية تالفة")]
    DamagedDisposal = 4
}
