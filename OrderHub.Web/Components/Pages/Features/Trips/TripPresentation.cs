using System;
using System.Collections.Generic;
using System.Globalization;
using MudBlazor;
using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Trips;

/// <summary>Status chips, action matrix and Arabic labels for standalone trips.</summary>
public static class TripPresentation
{
    public const string ActionEdit = "Edit";
    public const string ActionStart = "Start";
    public const string ActionComplete = "Complete";
    public const string ActionFail = "Fail";
    public const string ActionCancel = "Cancel";
    public const string ActionRetry = "Retry";

    public static (string Label, string Tone) GetStatusChip(TripDisplayState state) => state switch
    {
        TripDisplayState.New => ("جديد", "neutral"),
        TripDisplayState.InProgress => ("قيد التنفيذ", "processing"),
        TripDisplayState.AwaitingRetryStart => ("قيد التنفيذ — بانتظار البدء", "processing"),
        TripDisplayState.Failed => ("فشل", "failed"),
        TripDisplayState.Completed => ("مكتمل", "completed"),
        TripDisplayState.Cancelled => ("ملغي", "cancelled"),
        _ => ("غير معروف", "neutral")
    };

    public static string StatusTone(TripDisplayState state) => GetStatusChip(state).Tone;

    public static (string Label, string Tone) GetWorkRecordStatusChip(WorkRecordStatus status) => status switch
    {
        WorkRecordStatus.Pending => ("قيد الانتظار", "neutral"),
        WorkRecordStatus.Completed => ("مكتمل", "completed"),
        WorkRecordStatus.Cancelled => ("ملغي", "cancelled"),
        WorkRecordStatus.Failed => ("فشل", "failed"),
        _ => ("غير معروف", "neutral")
    };

    public static (string Label, string Tone) GetAttemptChip(WorkRecordStatus status, bool isStarted) =>
        (status, isStarted) switch
        {
            (WorkRecordStatus.Pending, false) => ("بانتظار البدء", "failed"),
            (WorkRecordStatus.Pending, true) => ("قيد التنفيذ", "processing"),
            (WorkRecordStatus.Completed, _) => ("مكتمل", "completed"),
            (WorkRecordStatus.Failed, _) => ("فشل", "failed"),
            (WorkRecordStatus.Cancelled, _) => ("ملغي", "cancelled"),
            _ => ("غير معروف", "neutral")
        };

    /// <summary>Solid chip (100/800) used by the template's attempt rows and history status transitions.</summary>
    public static string SolidChip(string tone) => tone switch
    {
        "completed" => "bg-emerald-100 text-emerald-800",
        "failed" => "bg-amber-100 text-amber-800",
        "cancelled" => "bg-rose-100 text-rose-800",
        "processing" => "bg-blue-100 text-blue-800",
        _ => "bg-slate-100 text-slate-700"
    };

    /// <summary>Attempt-number tile of the template's row list (newest attempt first).</summary>
    public static string AttemptTile(string tone) => tone switch
    {
        "completed" => "border-emerald-100 bg-emerald-50 text-emerald-700",
        "failed" => "border-amber-200 bg-amber-100 text-amber-800",
        "cancelled" => "border-rose-100 bg-rose-50 text-rose-700",
        "processing" => "border-blue-100 bg-blue-50 text-blue-700",
        _ => "border-slate-200 bg-slate-100 text-slate-700"
    };

    /// <summary>Work-record status tone for the history transition chips (app 4-hue palette).</summary>
    public static string WorkRecordTone(WorkRecordStatus? status) => status switch
    {
        WorkRecordStatus.Completed => "completed",
        WorkRecordStatus.Failed => "failed",
        WorkRecordStatus.Cancelled => "cancelled",
        _ => "neutral"
    };

    public static IReadOnlyList<string> GetAllowedActions(TripDisplayState state, bool hasWritePermission)
    {
        if (!hasWritePermission)
        {
            return Array.Empty<string>();
        }

        return state switch
        {
            TripDisplayState.New => [ActionEdit, ActionStart, ActionCancel],
            TripDisplayState.InProgress => [ActionComplete, ActionFail, ActionCancel],
            TripDisplayState.AwaitingRetryStart => [ActionStart, ActionCancel],
            TripDisplayState.Failed => [ActionRetry, ActionCancel],
            TripDisplayState.Completed => Array.Empty<string>(),
            TripDisplayState.Cancelled => Array.Empty<string>(),
            _ => Array.Empty<string>()
        };
    }

    public static string ActionLabel(string action) => action switch
    {
        ActionEdit => "تعديل المشوار",
        ActionStart => "بدء المشوار",
        ActionComplete => "إكمال المشوار",
        ActionFail => "تسجيل فشل المشوار",
        ActionCancel => "إلغاء المشوار",
        ActionRetry => "إعادة المحاولة",
        _ => "متابعة"
    };

    public static string ActionIcon(string action) => action switch
    {
        ActionEdit => Icons.Material.Filled.Edit,
        ActionStart => Icons.Material.Filled.PlayArrow,
        ActionComplete => Icons.Material.Filled.CheckCircleOutline,
        ActionFail => Icons.Material.Filled.ReportProblem,
        ActionCancel => Icons.Material.Filled.Cancel,
        ActionRetry => Icons.Material.Filled.Refresh,
        _ => Icons.Material.Filled.MoreVert
    };

    public static string ActionTone(string action) => action switch
    {
        ActionComplete => "completed",
        ActionFail => "failed",
        ActionCancel => "cancelled",
        _ => "processing"
    };

    public static bool RequiresReason(string action) =>
        action is ActionFail or ActionCancel;

    public static string ActionExplanation(string action, TripDisplayState state) => action switch
    {
        ActionStart when state == TripDisplayState.AwaitingRetryStart =>
            "سيتم بدء محاولة إعادة التشغيل المسجلة مسبقاً لهذا المشوار.",
        ActionStart => "سيتم إنشاء المحاولة الأولى وبدء احتساب المشوار كقيد التنفيذ. بعد البدء تُقفل بيانات المشوار ضد التعديل.",
        ActionComplete => "سيتم إغلاق المحاولة الجارية وتسجيل المشوار كمكتمل. لا يمكن التراجع عن هذه العملية.",
        ActionFail => "سيتم إغلاق المحاولة الجارية وتسجيل المشوار كفاشل مع حفظ السبب في سجل المشوار.",
        ActionCancel => "سيتم إلغاء المشوار نهائياً وتسجيل السبب في سجل المشوار. لا يمكن التراجع عن الإلغاء.",
        ActionRetry => "سيتم إنشاء محاولة جديدة بانتظار البدء دون بدء تلقائي؛ ابدأ المحاولة عند الجاهزية.",
        _ => "سيتم تسجيل العملية في سجل المشوار."
    };

    public static string SuccessMessage(string action) => action switch
    {
        ActionStart => "تم بدء المشوار بنجاح.",
        ActionComplete => "تم إكمال المشوار بنجاح.",
        ActionFail => "تم تسجيل فشل المشوار.",
        ActionCancel => "تم إلغاء المشوار.",
        ActionRetry => "تم إنشاء محاولة جديدة بانتظار البدء.",
        _ => "تمت العملية بنجاح."
    };

    public static string PartyTypeLabel(TripPartyType type) => type switch
    {
        TripPartyType.Client => "عميل",
        TripPartyType.Supplier => "مورد",
        TripPartyType.ShippingCarrier => "شركة شحن",
        TripPartyType.External => "خارجي",
        _ => "غير معروف"
    };

    public static string PartyTypeTone(TripPartyType type) =>
        type == TripPartyType.External ? "failed" : "processing";

    /// <summary>
    /// Per-position route-card accents on the details page («from» blue / «to» indigo),
    /// per the owner's 2026-11-09 template decision. Empty strings fall back to the party-type tone.
    /// </summary>
    public static (string TileClasses, string ChipClasses, string AccentClasses) RouteSlotTone(string? slot) => slot switch
    {
        "to" => ("bg-indigo-50 text-indigo-600 border-indigo-100",
                 "bg-indigo-50 text-indigo-700 border-indigo-200/80",
                 "text-indigo-600"),
        "from" => ("bg-blue-50 text-blue-600 border-blue-100",
                   "bg-blue-50 text-blue-700 border-blue-200/80",
                   "text-blue-600"),
        _ => (string.Empty, string.Empty, string.Empty)
    };

    /// <summary>Avatar initials: the first two letters of the first name (e.g. «إب», «خل»).</summary>
    public static string Initials(string name)
    {
        string trimmed = name?.Trim() ?? string.Empty;
        return trimmed.Length == 0 ? "—" : trimmed[..Math.Min(2, trimmed.Length)];
    }

    public static string EventLabel(StandaloneTripEventType eventType) => eventType switch
    {
        StandaloneTripEventType.Created => "إنشاء المشوار",
        StandaloneTripEventType.AttemptStarted => "بدء المحاولة",
        StandaloneTripEventType.Completed => "إكمال المشوار",
        StandaloneTripEventType.Failed => "فشل المشوار",
        StandaloneTripEventType.Cancelled => "إلغاء المشوار",
        StandaloneTripEventType.Retried => "إعادة المحاولة",
        StandaloneTripEventType.Reassigned => "إعادة إسناد المشوار",
        StandaloneTripEventType.Edited => "تعديل المشوار",
        _ => "حدث غير معروف"
    };

    public static string EventTone(StandaloneTripEventType eventType) => eventType switch
    {
        StandaloneTripEventType.Created => "neutral",
        StandaloneTripEventType.AttemptStarted => "processing",
        StandaloneTripEventType.Completed => "completed",
        StandaloneTripEventType.Failed => "failed",
        StandaloneTripEventType.Cancelled => "cancelled",
        StandaloneTripEventType.Retried => "processing",
        StandaloneTripEventType.Reassigned => "failed",
        StandaloneTripEventType.Edited => "processing",
        _ => "neutral"
    };

    /// <summary>Format: <c>#12 · 11/2026</c> with Latin digits (LTR fragment).</summary>
    public static string FormatTripNumber(long number, int year, int month) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "#{0} · {1}/{2}",
            number.ToString(CultureInfo.InvariantCulture),
            month.ToString("00", CultureInfo.InvariantCulture),
            year.ToString(CultureInfo.InvariantCulture));

    public static string AttemptProgress(int? currentAttemptNumber, int attemptCount) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "{0} / {1}",
            currentAttemptNumber?.ToString(CultureInfo.InvariantCulture) ?? "—",
            attemptCount.ToString(CultureInfo.InvariantCulture));
}

/// <summary>Dialog result: the new version, or a request to reload on a version conflict.</summary>
public sealed record TripActionOutcome(int? Version, bool ReloadRequired);
