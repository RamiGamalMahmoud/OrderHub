using MudBlazor;
using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Administration.Roles.Editor;

public static class RoleFeatureCatalog
{
    public static string Name(
        PermissionFeature feature)
    {
        return feature switch
        {
            PermissionFeature.Orders =>
                "الطلبات",

            PermissionFeature.Documents =>
                "المستندات",

            PermissionFeature.Products =>
                "المنتجات",

            PermissionFeature.Categories =>
                "الأقسام والتصنيفات",

            PermissionFeature.Properties =>
                "خصائص المنتجات",

            PermissionFeature.Clients =>
                "العملاء",

            PermissionFeature.Suppliers =>
                "الموردون",

            PermissionFeature.Deliverymen =>
                "المناديب",

            PermissionFeature.ShippingCarriers =>
                "شركات الشحن",

            PermissionFeature.Trips =>
                "المشاوير",

            PermissionFeature.Cities =>
                "المدن والمناطق",

            PermissionFeature.PaymentMethods =>
                "طرق الدفع",

            PermissionFeature.Warehouses =>
                "المخازن",

            PermissionFeature.Inventory =>
                "المخزون",

            PermissionFeature.Logs =>
                "سجلات النظام",

            PermissionFeature.Users =>
                "إدارة المستخدمين",

            PermissionFeature.Settings =>
                "إعدادات النظام",

            _ => feature.ToString()
        };
    }

    public static string Description(
        PermissionFeature feature)
    {
        return feature switch
        {
            PermissionFeature.Orders =>
                "عرض وإدارة الطلبات وسير العمل وتتبع حالات الدفع والتنفيذ",

            PermissionFeature.Documents =>
                "عروض الأسعار والفواتير والمستندات المالية والتقارير",

            PermissionFeature.Products =>
                "عرض وإدارة كتالوج المنتجات والمخزون وتسعير البنود",

            PermissionFeature.Categories =>
                "إدارة فئات وتصنيفات المنتجات والهيكل الشجري للأقسام",

            PermissionFeature.Properties =>
                "إدارة خصائص وخيارات المنتجات كالألوان والمقاسات",

            PermissionFeature.Clients =>
                "عرض وإدارة بيانات وحسابات العملاء وسجل المشتريات",

            PermissionFeature.Suppliers =>
                "عرض وإدارة الموردين وأوامر الشراء والحسابات الدائنة",

            PermissionFeature.Deliverymen =>
                "عرض وإدارة المناديب وتوزيع المهام الميدانية والتحصيل",

            PermissionFeature.ShippingCarriers =>
                "عرض وإدارة شركات الشحن وبوالص التوصيل وتتبع الشحنات",

            PermissionFeature.Trips =>
                "عرض جميع مشاوير المناديب وإدارة المشاوير المستقلة",

            PermissionFeature.Cities =>
                "إدارة المدن والنطاقات الجغرافية وتكاليف التوصيل",

            PermissionFeature.PaymentMethods =>
                "إدارة بوابات وطرق الدفع والرسوم والتحويلات البنكية",

            PermissionFeature.Warehouses =>
                "إدارة المخازن وأمناء المخزون وحالة التشغيل",

            PermissionFeature.Inventory =>
                "أرصدة المنتجات وحركات المخزون وأوامر الصرف",

            PermissionFeature.Logs =>
                "عرض سجلات التدقيق وتتبع حركات المستخدمين والأحداث",

            PermissionFeature.Users =>
                "إدارة حسابات المستخدمين وصلاحيات الدخول وتعيين الأدوار",

            PermissionFeature.Settings =>
                "إدارة الإعدادات العامة والربط البرمجي وتفضيلات التشغيل",

            _ => string.Empty
        };
    }

    public static string Icon(
        PermissionFeature feature)
    {
        return feature switch
        {
            PermissionFeature.Orders =>
                Icons.Material.Filled.ShoppingCart,

            PermissionFeature.Documents =>
                Icons.Material.Filled.Description,

            PermissionFeature.Products =>
                Icons.Material.Filled.Inventory2,

            PermissionFeature.Categories =>
                Icons.Material.Filled.Category,

            PermissionFeature.Properties =>
                Icons.Material.Filled.Tune,

            PermissionFeature.Clients =>
                Icons.Material.Filled.People,

            PermissionFeature.Suppliers =>
                Icons.Material.Filled.Store,

            PermissionFeature.Deliverymen =>
                Icons.Material.Filled.DeliveryDining,

            PermissionFeature.ShippingCarriers =>
                Icons.Material.Filled.LocalShipping,

            PermissionFeature.Trips =>
                Icons.Material.Filled.DirectionsBike,

            PermissionFeature.Cities =>
                Icons.Material.Filled.LocationCity,

            PermissionFeature.PaymentMethods =>
                Icons.Material.Filled.Payments,

            PermissionFeature.Warehouses =>
                Icons.Material.Filled.Warehouse,

            PermissionFeature.Inventory =>
                Icons.Material.Filled.Inventory,

            PermissionFeature.Logs =>
                Icons.Material.Filled.ReceiptLong,

            PermissionFeature.Users =>
                Icons.Material.Filled.ManageAccounts,

            PermissionFeature.Settings =>
                Icons.Material.Filled.Settings,

            _ =>
                Icons.Material.Filled.Security
        };
    }

    public static string Tone(
        PermissionFeature feature)
    {
        return feature switch
        {
            PermissionFeature.Orders =>
                "bg-blue-50 text-blue-600",

            PermissionFeature.Documents =>
                "bg-amber-50 text-amber-600",

            PermissionFeature.Products =>
                "bg-emerald-50 text-emerald-600",

            PermissionFeature.Categories =>
                "bg-indigo-50 text-indigo-600",

            PermissionFeature.Properties =>
                "bg-teal-50 text-teal-600",

            PermissionFeature.Clients =>
                "bg-pink-50 text-pink-600",

            PermissionFeature.Suppliers =>
                "bg-orange-50 text-orange-600",

            PermissionFeature.Deliverymen =>
                "bg-violet-50 text-violet-600",

            PermissionFeature.ShippingCarriers =>
                "bg-sky-50 text-sky-600",

            PermissionFeature.Trips =>
                "bg-cyan-50 text-cyan-600",

            PermissionFeature.Cities =>
                "bg-emerald-50 text-emerald-600",

            PermissionFeature.PaymentMethods =>
                "bg-green-50 text-green-600",

            PermissionFeature.Warehouses =>
                "bg-amber-50 text-amber-600",

            PermissionFeature.Inventory =>
                "bg-teal-50 text-teal-600",

            PermissionFeature.Logs =>
                "bg-amber-50 text-amber-600",

            PermissionFeature.Users =>
                "bg-blue-50 text-blue-600",

            PermissionFeature.Settings =>
                "bg-rose-50 text-rose-600",

            _ =>
                "bg-slate-50 text-slate-600"
        };
    }
}
