using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Orders.NotifyOrderParticipants.MessageBuilders;

internal static class NotificationTemplates
{
    public static string GetGreeting(RecipientType recipientType) => recipientType switch
    {
        RecipientType.Client => "{السلام عليكم ورحمة الله وبركاته|مرحباً بك|أهلاً بك عميلنا العزيز}، {يسعدنا إبلاغك بتحديث طلبك|تجد أدناه تفاصيل طلبك|نرسل لك بيان طلبك من متجر المساجد}:",
        RecipientType.Supplier => "{تحية طيبة وبعد،|مرحباً،|إلى قسم التجهيز:|برجاء العلم:} {يوجد أمر توريد جديد للطلب|يرجى تجهيز المنتجات التالية للطلب|إليكم بيان المنتجات المطلوبة للطلب} الخاص بـ (متجر المساجد):",
        RecipientType.Deliveryman or RecipientType.ShippingCarrier => "{مرحباً بك،|تحية طيبة،|إلى كابتن التوصيل:|} {لديك مهمة توصيل جديدة للطلب|برجاء استلام وتوصيل الطلب التالي|تجد أدناه بيانات طلب جديد جاهز للشحن}:",
        _ => "تفاصيل الطلب:"
    };

    public static string GetFooter(RecipientType recipientType) => recipientType switch
    {
        RecipientType.Client => "{" +
            "*مع تحيات متجر المساجد*|" +
            "*شكراً لثقتكم الغالية بمتجر المساجد*|" +
            "*طابت أوقاتكم - إدارة متجر المساجد*|" +
            "*سعداء بخدمتكم دائماً في متجر المساجد*|" +
            "*نشكركم على اختياركم متجر المساجد*" +
            "}",

        RecipientType.Supplier => "{" +
            "*برجاء التأكيد فور التجهيز - متجر المساجد*|" +
            "*شاكرين لكم حسن تعاونكم الدائم - متجر المساجد*|" +
            "*مع خالص التقدير والشكر - متجر المساجد*|" +
            "*قسم التنسيق والمتابعة - متجر المساجد*" +
            "}",

        RecipientType.Deliveryman or RecipientType.ShippingCarrier => "{" +
            "*برجاء المتابعة مع العميل بعد الاستلام - متجر المساجد*|" +
            "*بالتوفيق وكان الله في عونكم - متجر المساجد*|" +
            "*يرجى تحديث حالة التوصيل فور الوصول - متجر المساجد*|" +
            "*نشكر جهودكم معنا - إدارة متجر المساجد*" +
            "}",
        _ => "*متجر المساجد*"
    };

    public const string PRODUCTS_HEADER = "{*المنتجات*|*بيان البضاعة*|*تفاصيل الأصناف*|*قائمة المنتجات*}";
    public const string SUPPLIERP_RODUCTS_HEADER = "{*الأصناف المطلوبة:*|*بيان البضاعة المراد تجهيزها:*|*قائمة النواقص:*|*الأصناف:*}";
    public const string QUANTITY_LABEL = "{الكمية:|العدد:|المطلوب:}";
    public const string PRICE_LABEL = "{السعر:|القيمة:}";
    public const string TOTAL_LABEL = "{الإجمالي النهائي:|المطلوب سداده:|مجموع الحساب:}";
    public const string CLIENT_DESTINATION_LABEL = "{العميل المباشر|موقع العميل}";
    public const string CARRIER_DESTINATION_LABEL = "{شركة الشحن|مقر شركة الشحن}";
    public const string DELIVERYMAN_DESTINATION_LABEL = "{مندوب التوصيل|الكابتن المكلف}";
}
