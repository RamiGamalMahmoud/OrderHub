using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum DeliveryMethod
{
    [Description("استلام من المقر")]
    Pickup = 1,

    [Description("مندوب توصيل")]
    DeliveryMan = 2,

    [Description("شركة شحن")]
    ShippingCompany = 3,

    [Description("سلسلة توصيل")]
    DeliveryChain = 4
}

