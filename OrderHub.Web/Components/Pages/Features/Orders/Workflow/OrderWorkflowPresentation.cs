using System.Globalization;
using OrderHub.Domain.Enums;
using OrderHub.Application.Common.Extensions;

namespace OrderHub.Web.Components.Pages.Features.Orders.Workflow;

public static class OrderWorkflowPresentation
{
    public const string Neutral = "neutral";
    public const string TagBase = "inline-flex items-center gap-1.5 whitespace-nowrap rounded-lg border px-2.5 py-1 text-xs font-bold";

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
        OrderStatus.Processing => "processing",
        OrderStatus.ReadyForPickup => "ready",
        OrderStatus.OutForDelivery => "delivery",
        OrderStatus.Shipped => "shipped",
        OrderStatus.Delivered => "completed",
        OrderStatus.DeliveryFailed => "failed",
        OrderStatus.Cancelled => "cancelled",
        _ => Neutral
    };

    public static string StatusTone(DeliveryStatus status) => status switch
    {
        DeliveryStatus.InProgress => "delivery",
        DeliveryStatus.Completed => "completed",
        DeliveryStatus.Failed => "failed",
        DeliveryStatus.Cancelled => "cancelled",
        _ => Neutral
    };

    public static string HistoryTone(string label, bool step) => step
        ? System.Enum.GetValues<DeliveryStatus>().Where(status => status.GetDescription() == label).Select(status => StatusTone(status)).FirstOrDefault() ?? Neutral
        : System.Enum.GetValues<OrderStatus>().Where(status => status.GetDescription() == label).Select(status => StatusTone(status)).FirstOrDefault() ?? Neutral;

    /// <summary>Static tag classes (a full literal set per tone so Tailwind can detect them).</summary>
    public static string Tag(string tone) => tone switch
    {
        "processing" => "border-violet-200 bg-violet-50 text-violet-700",
        "ready" => "border-amber-200 bg-amber-50 text-amber-800",
        "delivery" => "border-brand-200 bg-brand-50 text-brand-700",
        "shipped" => "border-cyan-200 bg-cyan-50 text-cyan-700",
        "completed" => "border-emerald-200 bg-emerald-50 text-emerald-700",
        "failed" => "border-orange-200 bg-orange-50 text-orange-800",
        "cancelled" => "border-red-200 bg-red-50 text-red-700",
        _ => "border-slate-200 bg-slate-100 text-slate-600"
    };

    public static string Tag(OrderStatus status) => Tag(StatusTone(status));
    public static string Tag(DeliveryStatus status) => Tag(StatusTone(status));

    /// <summary>Tone-colored button (works on MudButton and plain buttons).</summary>
    public static string Action(string tone) => tone switch
    {
        "processing" => "border border-violet-200 bg-violet-50 text-violet-700 hover:bg-violet-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        "ready" => "border border-amber-200 bg-amber-50 text-amber-800 hover:bg-amber-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        "delivery" => "border border-brand-200 bg-brand-50 text-brand-700 hover:bg-brand-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        "shipped" => "border border-cyan-200 bg-cyan-50 text-cyan-700 hover:bg-cyan-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        "completed" => "border border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        "failed" => "border border-orange-200 bg-orange-50 text-orange-800 hover:bg-orange-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        "cancelled" => "border border-red-200 bg-red-50 text-red-700 hover:bg-red-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        _ => "border border-slate-200 bg-slate-50 text-slate-700 hover:bg-slate-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400"
    };

    public static string Action(OrderStatus status) => Action(StatusTone(status));
    public static string Action(DeliveryStatus status) => Action(StatusTone(status));

    /// <summary>Logical start border for step cards.</summary>
    public static string Accent(string tone) => tone switch
    {
        "processing" => "border-s-violet-400",
        "ready" => "border-s-amber-400",
        "delivery" => "border-s-brand-400",
        "shipped" => "border-s-cyan-400",
        "completed" => "border-s-emerald-400",
        "failed" => "border-s-orange-400",
        "cancelled" => "border-s-red-400",
        _ => "border-s-slate-300"
    };

    public static string Dot(string tone) => tone switch
    {
        "processing" => "bg-violet-500",
        "ready" => "bg-amber-500",
        "delivery" => "bg-brand-500",
        "shipped" => "bg-cyan-500",
        "completed" => "bg-emerald-500",
        "failed" => "bg-orange-500",
        "cancelled" => "bg-red-500",
        _ => "bg-slate-400"
    };

    public static string IconTile(string tone) => tone switch
    {
        "processing" => "bg-violet-50 text-violet-600",
        "ready" => "bg-amber-50 text-amber-600",
        "delivery" => "bg-brand-50 text-brand-600",
        "shipped" => "bg-cyan-50 text-cyan-600",
        "completed" => "bg-emerald-50 text-emerald-600",
        "failed" => "bg-orange-50 text-orange-600",
        "cancelled" => "bg-red-50 text-red-600",
        _ => "bg-slate-100 text-slate-500"
    };

    /// <summary>Soft bordered surface for stage/step containers.</summary>
    public static string Surface(string tone) => tone switch
    {
        "processing" => "border-violet-200 bg-violet-50/60",
        "ready" => "border-amber-200 bg-amber-50/60",
        "delivery" => "border-brand-200 bg-brand-50/60",
        "shipped" => "border-cyan-200 bg-cyan-50/60",
        "completed" => "border-emerald-200 bg-emerald-50/60",
        "failed" => "border-orange-200 bg-orange-50/60",
        "cancelled" => "border-red-200 bg-red-50/60",
        _ => "border-slate-200 bg-slate-50/60"
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

    public static OrderStatus[] Stages(DeliveryMethod? method) => method switch
    {
        DeliveryMethod.Pickup => [OrderStatus.Processing, OrderStatus.ReadyForPickup, OrderStatus.Delivered],
        DeliveryMethod.ShippingCompany => [OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered],
        DeliveryMethod.DeliveryMan or DeliveryMethod.DeliveryChain => [OrderStatus.Processing, OrderStatus.OutForDelivery, OrderStatus.Delivered],
        _ => [OrderStatus.Processing]
    };

    /// <summary>Elapsed operational time with Latin digits (InvariantCulture).</summary>
    public static string DurationLabel(DateTime fromUtc, DateTime toUtc)
    {
        var span = toUtc > fromUtc ? toUtc - fromUtc : TimeSpan.Zero;
        if (span.TotalMinutes < 1) return "أقل من دقيقة";
        if (span.TotalHours < 1) return $"{span.TotalMinutes.ToString("0", CultureInfo.InvariantCulture)} دقيقة";
        if (span.TotalDays < 1) return $"{span.TotalHours.ToString("0", CultureInfo.InvariantCulture)} ساعة";
        return $"{span.TotalDays.ToString("0", CultureInfo.InvariantCulture)} يوم";
    }
}
