using System.ComponentModel;

namespace OrderHub.Domain.Enums;

public enum OrderHistorySource
{
    [Description("إنشاء")]
    Creation,
    [Description("إجراء يدوي")]
    ManualAction,
    [Description("إعادة محاولة")]
    Retry,
    [Description("اكتمال آخر خطوة")]
    LastStepCompleted,
    [Description("تسليم مباشر")]
    DirectDelivery,
    [Description("إلغاء الطلب")]
    OrderCancellation,
    [Description("ترحيل بيانات قديمة")]
    LegacyMigration,
    [Description("إعادة توجيه التوصيل")]
    DeliveryRouteReassigned,
    [Description("تأكيد استلام")]
    PickupConfirmed,
    [Description("إلغاء خطوة توصيل")]
    DeliveryStepCancelled,
    [Description("تحرير حجز المخزون")]
    StockRelease
}
