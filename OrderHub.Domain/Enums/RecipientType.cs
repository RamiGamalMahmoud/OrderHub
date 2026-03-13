using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum RecipientType
{
    [Description("عميل")]
    Client,

    [Description("شركة شحن")]
    Carrier,

    [Description("مورد")]
    Supplier,

    [Description("مندوب توصيل")]
    Deliveryman
}