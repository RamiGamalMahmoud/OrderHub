using OrderHub.Domain.Enums;
using OrderHub.Application.Common.Extensions;

namespace OrderHub.Web.Components.Pages.Features.Orders.Workflow;

public static class OrderWorkflowPresentation
{
    public static string LegacyStatusLabel(string value) => value == "Pending" ? "قيد الانتظار"
        : System.Enum.TryParse<OrderStatus>(value, out var status) && System.Enum.IsDefined(status)
            ? OrderHub.Application.Common.Extensions.EnumExtensions.GetDescription(status) : "حالة قديمة غير معروفة";
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

    public static string StatusTone(OrderStatus status) => status switch
    {
        OrderStatus.Processing => "order-tone-processing",
        OrderStatus.ReadyForPickup => "order-tone-ready",
        OrderStatus.OutForDelivery => "order-tone-delivery",
        OrderStatus.Shipped => "order-tone-shipped",
        OrderStatus.Delivered => "order-tone-completed",
        OrderStatus.DeliveryFailed => "order-tone-failed",
        OrderStatus.Cancelled => "order-tone-cancelled",
        _ => "order-tone-neutral"
    };

    public static string StatusTone(DeliveryStatus status) => status switch
    {
        DeliveryStatus.InProgress => "order-tone-delivery",
        DeliveryStatus.Completed => "order-tone-completed",
        DeliveryStatus.Failed => "order-tone-failed",
        DeliveryStatus.Cancelled => "order-tone-cancelled",
        _ => "order-tone-neutral"
    };

    public static string HistoryTone(string label, bool step) => step
        ? System.Enum.GetValues<DeliveryStatus>().Where(status => status.GetDescription() == label).Select(status => StatusTone(status)).FirstOrDefault() ?? "order-tone-neutral"
        : System.Enum.GetValues<OrderStatus>().Where(status => status.GetDescription() == label).Select(status => StatusTone(status)).FirstOrDefault() ?? "order-tone-neutral";

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

    public static OrderStatus[] Stages(DeliveryMethod? method) => method switch
    {
        DeliveryMethod.Pickup => [OrderStatus.Processing, OrderStatus.ReadyForPickup, OrderStatus.Delivered],
        DeliveryMethod.ShippingCompany => [OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered],
        DeliveryMethod.DeliveryMan or DeliveryMethod.DeliveryChain => [OrderStatus.Processing, OrderStatus.OutForDelivery, OrderStatus.Delivered],
        _ => [OrderStatus.Processing]
    };
}
