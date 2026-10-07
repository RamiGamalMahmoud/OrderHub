using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum OrderStatus
{
    [Description("قيد التجهيز")]
    Processing = 1,

    [Description("تم الشحن")]
    Shipped = 2,

    [Description("تم التسليم")]
    Delivered = 3,

    [Description("تم الإلغاء")]
    Cancelled = 4,

    [Description("جاهز للتسليم")]
    ReadyForPickup = 5,

    [Description("خرج للتوصيل")]
    OutForDelivery = 6,

    [Description("تعذر التسليم")]
    DeliveryFailed = 7
}
