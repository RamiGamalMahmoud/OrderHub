using MudBlazor;
using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Orders.Workflow;

public static class OrderWorkflowPresentation
{
    public static string ActionLabel(OrderStatus target, OrderStatus current = OrderStatus.Processing) => target switch
    {
        OrderStatus.ReadyForPickup => "جاهز لاستلام العميل",
        OrderStatus.OutForDelivery when current == OrderStatus.DeliveryFailed => "إعادة محاولة التوصيل",
        OrderStatus.OutForDelivery => "بدء التوصيل",
        OrderStatus.Shipped when current == OrderStatus.DeliveryFailed => "إعادة محاولة الشحن",
        OrderStatus.Shipped => "تسجيل تسليم الطلب لشركة الشحن",
        OrderStatus.Delivered => "تأكيد تسليم الطلب",
        OrderStatus.DeliveryFailed => "تسجيل تعذر التسليم",
        OrderStatus.Cancelled => "إلغاء الطلب",
        _ => "متابعة"
    };

    public static string StepActionLabel(DeliveryStatus target, DeliveryStatus current) => target switch
    {
        DeliveryStatus.InProgress when current == DeliveryStatus.Failed => "إعادة المحاولة",
        DeliveryStatus.InProgress => "بدء الخطوة",
        DeliveryStatus.Completed => "تأكيد إكمال الخطوة",
        DeliveryStatus.Failed => "تسجيل تعذر التنفيذ",
        _ => "متابعة"
    };

    public static Color StatusColor(OrderStatus status) => status switch
    {
        OrderStatus.Delivered => Color.Success,
        OrderStatus.DeliveryFailed => Color.Warning,
        OrderStatus.Cancelled => Color.Error,
        _ => Color.Primary
    };

    public static string Instruction(OrderStatus status, DeliveryMethod? method) => status switch
    {
        OrderStatus.Processing => "راجع بيانات الطلب وخطة التوصيل. بعد بدء التنفيذ ستُقفل بيانات الطلب ضد التعديل.",
        OrderStatus.ReadyForPickup => "عند استلام العميل للطلب، أكد عملية التسليم.",
        OrderStatus.OutForDelivery when method == DeliveryMethod.DeliveryChain => "نفّذ الخطوة المتاحة بالترتيب. الخطوات التالية تنتظر اكتمال السابقة.",
        OrderStatus.OutForDelivery => "عند انتهاء محاولة التوصيل، سجل نتيجتها: تسليم أو تعذر.",
        OrderStatus.Shipped => "سجل نتيجة التسليم عند توفرها من شركة الشحن.",
        OrderStatus.DeliveryFailed => "راجع سبب تعذر المحاولة، ثم ابدأ محاولة جديدة أو ألغِ الطلب.",
        OrderStatus.Delivered => "تم تسليم الطلب، وأُغلق تنفيذ جميع خطواته. لا توجد عمليات تشغيل إضافية.",
        OrderStatus.Cancelled => "تم إلغاء الطلب. لا يمكن تعديله أو إعادة تشغيله.",
        _ => "بيانات الطلب تحتاج مراجعة."
    };

    public static string[] Stages(DeliveryMethod? method) => method switch
    {
        DeliveryMethod.Pickup => ["قيد التجهيز", "جاهز للتسليم", "تم التسليم"],
        DeliveryMethod.ShippingCompany => ["قيد التجهيز", "تم الشحن", "تم التسليم"],
        DeliveryMethod.DeliveryMan or DeliveryMethod.DeliveryChain => ["قيد التجهيز", "خرج للتوصيل", "تم التسليم"],
        _ => ["قيد التجهيز"]
    };
}
