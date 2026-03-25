using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum WhatsappGroupType
{
    [Description("موردين")]
    Suppliers = 1,

    [Description("مندوبين توصيل")]
    Deliverymen = 2
}
