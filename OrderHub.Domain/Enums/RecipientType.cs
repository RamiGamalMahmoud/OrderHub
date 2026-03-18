using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum RecipientType
{
    [Description("عميل")]
    Client = 1,

    [Description("شركة شحن")]
    ShippingCarrier = 2,

    [Description("مورد")]
    Supplier = 3,

    [Description("مندوب توصيل")]
    Deliveryman = 4
}