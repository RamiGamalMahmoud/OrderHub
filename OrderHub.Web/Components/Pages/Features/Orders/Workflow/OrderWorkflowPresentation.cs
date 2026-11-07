using System.Globalization;
using OrderHub.Domain.Enums;
using OrderHub.Application.Common.Extensions;

namespace OrderHub.Web.Components.Pages.Features.Orders.Workflow;

public static class OrderWorkflowPresentation
{
    public const string Neutral = "neutral";
    public const string TagBase = "inline-flex items-center gap-1 whitespace-nowrap rounded-md border px-2 py-0.5 text-[11px] font-bold";

    /// <summary>Compact inner section card (matches the order/product editor language).</summary>
    public const string Card = "rounded-xl border border-slate-200/90 bg-white p-4 shadow-xs sm:p-5";
    /// <summary>Section header row: icon tile + title + helper, separated by a hairline.</summary>
    public const string CardHeader = "mb-3 flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-3";
    /// <summary>32px tonal icon tile used in section headers.</summary>
    public const string IconTileBase = "inline-flex h-8 w-8 shrink-0 items-center justify-center rounded-lg border";
    /// <summary>Soft inner tile for read-only stats.</summary>
    public const string StatTile = "rounded-lg bg-slate-50 px-3 py-2";

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

    /// <summary>Source-driven tone wins over the status text for movement-style history rows.</summary>
    public static string HistoryTone(string label, bool step, OrderHistorySource source) => source switch
    {
        OrderHistorySource.DeliveryRouteReassigned => "processing",
        OrderHistorySource.DeliveryStepCancelled => "cancelled",
        OrderHistorySource.PickupConfirmed => "completed",
        _ => HistoryTone(label, step)
    };

    /// <summary>Static tag classes (a full literal set per tone so Tailwind can detect them).
    /// Palette is deliberately reduced to meaning: brand = active, emerald = done,
    /// amber = needs attention, rose = terminated, slate = not started / neutral.</summary>
    public static string Tag(string tone) => tone switch
    {
        "processing" => "border-brand-200 bg-brand-50 text-brand-700",
        "ready" => "border-brand-200 bg-brand-50 text-brand-700",
        "delivery" => "border-brand-200 bg-brand-50 text-brand-700",
        "shipped" => "border-brand-200 bg-brand-50 text-brand-700",
        "completed" => "border-emerald-200 bg-emerald-50 text-emerald-700",
        "failed" => "border-amber-200 bg-amber-50 text-amber-800",
        "cancelled" => "border-rose-200 bg-rose-50 text-rose-700",
        _ => "border-slate-200 bg-slate-100 text-slate-600"
    };

    public static string Tag(OrderStatus status) => Tag(StatusTone(status));
    public static string Tag(DeliveryStatus status) => Tag(StatusTone(status));

    /// <summary>Tone-colored button (works on MudButton and plain buttons).</summary>
    public static string Action(string tone) => tone switch
    {
        "processing" => "border border-brand-200 bg-brand-50 text-brand-700 hover:bg-brand-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        "ready" => "border border-brand-200 bg-brand-50 text-brand-700 hover:bg-brand-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        "delivery" => "border border-brand-200 bg-brand-50 text-brand-700 hover:bg-brand-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        "shipped" => "border border-brand-200 bg-brand-50 text-brand-700 hover:bg-brand-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        "completed" => "border border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        "failed" => "border border-amber-200 bg-amber-50 text-amber-800 hover:bg-amber-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        "cancelled" => "border border-rose-200 bg-rose-50 text-rose-700 hover:bg-rose-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400",
        _ => "border border-slate-200 bg-slate-50 text-slate-700 hover:bg-slate-100 disabled:border-slate-200 disabled:bg-slate-50 disabled:text-slate-400"
    };

    public static string Action(OrderStatus status) => Action(StatusTone(status));
    public static string Action(DeliveryStatus status) => Action(StatusTone(status));

    /// <summary>Logical start border for step cards.</summary>
    public static string Accent(string tone) => tone switch
    {
        "processing" => "border-s-brand-400",
        "ready" => "border-s-brand-400",
        "delivery" => "border-s-brand-400",
        "shipped" => "border-s-brand-400",
        "completed" => "border-s-emerald-400",
        "failed" => "border-s-amber-400",
        "cancelled" => "border-s-rose-400",
        _ => "border-s-slate-300"
    };

    public static string Dot(string tone) => tone switch
    {
        "processing" => "bg-brand-500",
        "ready" => "bg-brand-500",
        "delivery" => "bg-brand-500",
        "shipped" => "bg-brand-500",
        "completed" => "bg-emerald-500",
        "failed" => "bg-amber-500",
        "cancelled" => "bg-rose-500",
        _ => "bg-slate-400"
    };

    public static string IconTile(string tone) => tone switch
    {
        "processing" => "border-brand-100 bg-brand-50 text-brand-600",
        "ready" => "border-brand-100 bg-brand-50 text-brand-600",
        "delivery" => "border-brand-100 bg-brand-50 text-brand-600",
        "shipped" => "border-brand-100 bg-brand-50 text-brand-600",
        "completed" => "border-emerald-100 bg-emerald-50 text-emerald-600",
        "failed" => "border-amber-100 bg-amber-50 text-amber-600",
        "cancelled" => "border-rose-100 bg-rose-50 text-rose-600",
        _ => "border-slate-200 bg-slate-100 text-slate-500"
    };

    /// <summary>Soft bordered surface for stage/step containers.</summary>
    public static string Surface(string tone) => tone switch
    {
        "processing" => "border-brand-200 bg-brand-50/60",
        "ready" => "border-brand-200 bg-brand-50/60",
        "delivery" => "border-brand-200 bg-brand-50/60",
        "shipped" => "border-brand-200 bg-brand-50/60",
        "completed" => "border-emerald-200 bg-emerald-50/60",
        "failed" => "border-amber-200 bg-amber-50/60",
        "cancelled" => "border-rose-200 bg-rose-50/60",
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
