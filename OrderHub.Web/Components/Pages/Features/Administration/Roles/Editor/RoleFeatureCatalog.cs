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
                "الأقسام",

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

            PermissionFeature.Cities =>
                "المدن",

            PermissionFeature.PaymentMethods =>
                "طرق الدفع",

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
                "عرض وإدارة الطلبات",

            PermissionFeature.Documents =>
                "عروض الأسعار والفواتير والمستندات",

            PermissionFeature.Products =>
                "عرض وإدارة المنتجات",

            PermissionFeature.Categories =>
                "إدارة أقسام المنتجات",

            PermissionFeature.Properties =>
                "إدارة خصائص وخيارات المنتجات",

            PermissionFeature.Clients =>
                "عرض وإدارة بيانات العملاء",

            PermissionFeature.Suppliers =>
                "عرض وإدارة الموردين",

            PermissionFeature.Deliverymen =>
                "عرض وإدارة المناديب",

            PermissionFeature.ShippingCarriers =>
                "عرض وإدارة شركات الشحن",

            PermissionFeature.Cities =>
                "إدارة المدن",

            PermissionFeature.PaymentMethods =>
                "إدارة طرق الدفع",

            PermissionFeature.Logs =>
                "عرض سجلات النظام",

            PermissionFeature.Users =>
                "إدارة حسابات المستخدمين",

            PermissionFeature.Settings =>
                "إدارة إعدادات النظام",

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

            PermissionFeature.Cities =>
                Icons.Material.Filled.LocationCity,

            PermissionFeature.PaymentMethods =>
                Icons.Material.Filled.Payments,

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
                "bg-blue-50 text-blue-600 border border-blue-100",

            PermissionFeature.Documents =>
                "bg-amber-50 text-amber-600 border border-amber-100",

            PermissionFeature.Products =>
                "bg-violet-50 text-violet-600 border border-violet-100",

            PermissionFeature.Categories =>
                "bg-emerald-50 text-emerald-600 border border-emerald-100",

            PermissionFeature.Properties =>
                "bg-cyan-50 text-cyan-600 border border-cyan-100",

            PermissionFeature.Clients =>
                "bg-rose-50 text-rose-600 border border-rose-100",

            PermissionFeature.Suppliers =>
                "bg-teal-50 text-teal-600 border border-teal-100",

            PermissionFeature.Deliverymen =>
                "bg-indigo-50 text-indigo-600 border border-indigo-100",

            PermissionFeature.ShippingCarriers =>
                "bg-sky-50 text-sky-600 border border-sky-100",

            PermissionFeature.Cities =>
                "bg-lime-50 text-lime-700 border border-lime-100",

            PermissionFeature.PaymentMethods =>
                "bg-green-50 text-green-600 border border-green-100",

            PermissionFeature.Logs =>
                "bg-orange-50 text-orange-600 border border-orange-100",

            PermissionFeature.Users =>
                "bg-red-50 text-red-600 border border-red-100",

            PermissionFeature.Settings =>
                "bg-zinc-50 text-zinc-600 border border-zinc-100",

            _ =>
                "bg-slate-50 text-slate-600 border border-slate-100"
        };
    }
}
