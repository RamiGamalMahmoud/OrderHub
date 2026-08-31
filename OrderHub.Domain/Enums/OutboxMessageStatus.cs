using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum OutboxMessageStatus
{
    [Description("قيد الانتظار")]
    Pending = 1,

    [Description("تم الارسال")]
    Sent = 2,

    [Description("فشل")]
    Failed = 3,

    [Description("إرسال")]
    Sending = 4,
}
