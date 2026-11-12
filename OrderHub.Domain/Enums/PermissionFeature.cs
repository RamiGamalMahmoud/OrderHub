using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum PermissionFeature
{
    // Orders
    [Description("الطلبات")]
    Orders,

    // Documents
    [Description("المستندات")]
    Documents,

    // Catalog
    [Description("المنتجات")]
    Products,

    [Description("الأقسام")]
    Categories,

    [Description("خصائص المنتجات")]
    Properties,

    // Business Partners
    [Description("العملاء")]
    Clients,

    [Description("الموردون")]
    Suppliers,

    // Logistics
    [Description("المناديب")]
    Deliverymen,

    [Description("شركات الشحن")]
    ShippingCarriers,

    [Description("المشاوير")]
    Trips,

    // Reference Data
    [Description("المدن")]
    Cities,

    [Description("طرق الدفع")]
    PaymentMethods,

    // Inventory
    [Description("المخازن")]
    Warehouses,

    [Description("المخزون")]
    Inventory,

    // Administration
    [Description("سجلات النظام")]
    Logs,

    [Description("إدارة المستخدمين")]
    Users,

    [Description("إعدادات النظام")]
    Settings
}