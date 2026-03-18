using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum DeliveryStatus
{
    [Description("قيد الانتظار")]
    Pending,

    [Description("قيد التنفيذ")]
    InProgress,

    [Description("مكتمل")]
    Completed
}
