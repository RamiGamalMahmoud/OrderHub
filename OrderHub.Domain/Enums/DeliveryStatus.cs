using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum DeliveryStatus
{
    [Description("قيد الانتظار")]
    Pending,

    [Description("قيد التسليم")]
    InProgress,

    [Description("تم التنفيذ")]
    Completed,

    [Description("تعذر التنفيذ")]
    Failed,

    [Description("ملغاة")]
    Cancelled
}
