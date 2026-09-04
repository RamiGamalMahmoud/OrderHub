using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum OrderStatus
{
    [Description("قيد الانتظار")]
    Pending,

    [Description("قيد التجهيز")]
    Processing,

    [Description("تم الشحن")]
    Shipped,

    [Description("تم التوصيل")]
    Delivered,

    [Description("ملغى")]
    Cancelled
}
