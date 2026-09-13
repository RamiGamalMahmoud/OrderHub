using System.ComponentModel;

namespace OrderHub.Domain.Enums;
public enum PermissionFeature
{
    [Description("الطلبات")]
    Orders = 1,

    [Description("المستندات")]
    Documents = 2,

    [Description("المنتجات")]
    Products = 3,

    [Description("الأقسام")]
    Categories = 4,

    [Description("الخصائص")]
    Properties = 5,

    [Description("العملاء")]
    Clients = 6,

    [Description("الموردون")]
    Suppliers = 7,

    [Description("المناديب")]
    Deliverymen = 8,

    [Description("شركات الشحن")]
    ShippingCarriers = 9,

    [Description("المدن")]
    Cities = 10,

    [Description("طرق الدفع")]
    PaymentMethods = 11,

    [Description("مجموعات الواتساب")]
    WhatsAppGroups = 12,

    [Description("سجل الرسائل")]
    Messages = 13,

    [Description("سجلات النظام")]
    Logs = 14,

    [Description("إدارة المستخدمين")]
    Users = 15,

    [Description("إعدادات النظام")]
    Settings = 16
}