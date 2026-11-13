using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum InventoryApprovalStatus
{
    [Description("قيد الاعتماد")]
    Pending = 1,

    [Description("معتمد")]
    Approved = 2,

    [Description("مرفوض")]
    Rejected = 3,

    [Description("متقادم")]
    Stale = 4
}
