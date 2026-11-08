using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OrderHub.Domain.Services.Pricing;

namespace OrderHub.Application.Features.Orders.GetSummaries;

/// <summary>Builds the per-recipient dispatch messages and their destination details from persisted data.</summary>
public static class OrderSummaryTextBuilder
{
    private static readonly string[] ArabicMonths =
    ["يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"];

    public static IReadOnlyList<OrderSummaryAudience> Build(OrderSummaryData data)
    {
        var isCash = string.Equals(data.PaymentMethodCode, "CASH", StringComparison.OrdinalIgnoreCase);
        var paymentLabel = isCash
            ? "عند الاستلام"
            : string.IsNullOrWhiteSpace(data.PaymentMethodName) ? "غير محدد" : data.PaymentMethodName;
        var collectionAmount = isCash ? data.InvoiceTotal : (decimal?)null;

        var audiences = new List<OrderSummaryAudience>
        {
            new("customer", 0, Display(data.CustomerName), "العميل", "عميل معتمد",
                data.CustomerPhone, Display(data.CustomerAddress), paymentLabel, collectionAmount,
                [new OrderSummaryText("customer", 0, data.CustomerName,
                    $"طلب رقم {data.OrderNumber}", BuildCustomer(data))])
        };

        var deliveryGroups = data.DeliveryRecords
            .GroupBy(record => (Type: record.Kind == "مشوار" ? "deliveryman" : "shipping", record.PartyId))
            .ToList();

        foreach (var group in deliveryGroups.Where(group => group.Key.Type == "deliveryman"))
        {
            var records = group.ToList();
            var first = records[0];
            var messages = records.Select(record => new OrderSummaryText(
                "deliveryman", record.PartyId, record.PartyName,
                $"{record.Kind} {record.Number} — {Month(record.Month)} {record.Year}",
                BuildDeliveryman(data, record, isCash, paymentLabel))).ToList();

            audiences.Add(new("deliveryman", first.PartyId, Display(first.PartyName),
                "مندوب التوصيل", "سائق التوصيل", PhoneOf(records),
                Display(data.CustomerAddress), paymentLabel, collectionAmount, messages));
        }

        foreach (var record in data.SupplierRecords)
        {
            var title = $"توريد {record.Number} — {Month(record.Month)} {record.Year}";
            audiences.Add(new("supplier", record.SupplierId, Display(record.SupplierName),
                "المورد", "مورد معتمد", record.SupplierPhone, Display(record.SupplierAddress), null, null,
                [new OrderSummaryText("supplier", record.SupplierId, record.SupplierName, title, BuildSupplier(data, record))]));
        }

        foreach (var group in deliveryGroups.Where(group => group.Key.Type == "shipping"))
        {
            var records = group.ToList();
            var first = records[0];
            audiences.Add(new("shipping", first.PartyId, Display(first.PartyName),
                "شركة الشحن", "شركة الشحن", PhoneOf(records), DestinationCity(data), null, null,
                [new OrderSummaryText("shipping", first.PartyId, first.PartyName,
                    $"طلب رقم {data.OrderNumber}", BuildCarrier(data, isCash, paymentLabel))]));
        }

        return audiences;
    }

    private static string BuildCustomer(OrderSummaryData data) =>
        $"مرحباً بك يا {Display(data.CustomerName)} 👋\n" +
        $"تم تجهيز طلبك رقم: *{data.OrderNumber}* بنجاح!\n\n" +
        "📦 *محتويات الطلب:*\n" + CustomerLines(data.Items) + "\n\n" +
        $"💰 *إجمالي الفاتورة شامل الضريبة:* {Amount(data.InvoiceTotal)} ر.س\n" +
        $"📍 *عنوان التسليم:* {Display(data.CustomerAddress)}\n\n" +
        "شكراً لتسوقك معنا! 🌟";

    private static string BuildDeliveryman(OrderSummaryData data, PartySummaryRecord record, bool isCash, string paymentLabel)
    {
        var title = $"{record.Kind} {record.Number} — {Month(record.Month)} {record.Year}";
        var text = "مهمة توصيل جديدة 🚚\n" +
                   $"*سجل المشوار:* {title}\n" +
                   $"*رقم الطلب:* {data.OrderNumber}";
        if (record.StepOrder.HasValue)
            text += $"\n*المسار:* {Display(record.From)} ← {Display(record.To)}";

        text += "\n\n👤 *بيانات العميل:*" +
                $"\n• الاسم: {Display(data.CustomerName)}" +
                $"\n• رقم الجوال: {Display(data.CustomerPhone)}" +
                $"\n• العنوان: {Display(data.CustomerAddress)}" +
                "\n\n📦 *الأصناف المطلوب تسليمها:*\n" + ItemQuantities(data.Items) +
                "\n\n💵 " + (isCash
                    ? $"*المبلغ المطلوب تحصيله نقداً / مدى:*\n*{Amount(data.InvoiceTotal)} ر.س*"
                    : $"*طريقة الدفع:* {paymentLabel} — لا يوجد تحصيل نقدي.");
        return text;
    }

    private static string BuildSupplier(OrderSummaryData data, SupplierSummaryRecord record)
    {
        var title = $"أمر صرف توريد {record.Number} — {Month(record.Month)} {record.Year}";
        var items = record.Items.Count == 1
            ? $"📦 *الصنف:* {record.Items[0].ProductName}\n🔢 *الكمية المطلوبة:* {record.Items[0].Quantity} قطعة"
            : "📦 *الأصناف:*\n" + string.Join("\n", record.Items.Select(item => $"• {item.ProductName} — الكمية: {item.Quantity}"));

        return $"أمر توريد وتجهيز بضاعة 🏭\n*{title}*\n" +
               $"مرتبط بالطلب: *{data.OrderNumber}*\n" +
               $"تاريخ الطلب: {data.OrderDate.ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}\n\n" +
               "المطلوب تجهيزه من طرفكم فقط:\n" + items + "\n\n" +
               "📍 *جهة الاستلام:* مندوب شركة OrderHub\n" +
               "⚠️ يُرجى إرسال إشعار الجاهزية فور اكتمال التجهيز.";
    }

    private static string BuildCarrier(OrderSummaryData data, bool isCash, string paymentLabel)
    {
        var city = DestinationCity(data);
        return "طلب بوليصة شحن مستعجل 📦\n" +
               $"*رقم المرجع:* {data.OrderNumber}\n\n" +
               $"👤 *المستلم:* {Display(data.CustomerName)}\n" +
               $"📞 *هاتف المستلم:* {Display(data.CustomerPhone)}\n" +
               $"📍 *المدينة:* {Display(city)}\n\n" +
               "💰 *شحن مع تحصيل COD:* " + (isCash
                   ? $"نعم ({Amount(data.InvoiceTotal)} ر.س)"
                   : $"لا — طريقة الدفع: {paymentLabel}");
    }

    private static string CustomerLines(IEnumerable<OrderSummaryLine> items) => string.Join("\n", items.Select(item =>
    {
        var pricing = PricingCalculator.CalculateItem(new PricingItem(0, item.ProductName, item.Quantity, item.UnitPrice, item.VatRate));
        return $"• {item.ProductName} – الكمية: {item.Quantity} (السعر: {Amount(pricing.Total)} ر.س)";
    }));

    private static string ItemQuantities(IEnumerable<OrderSummaryLine> items) =>
        string.Join("\n", items.Select(item => $"• {item.ProductName} (الكمية: {item.Quantity})"));

    private static string PhoneOf(IReadOnlyList<PartySummaryRecord> records) =>
        records.Select(record => record.PartyPhone).FirstOrDefault(phone => !string.IsNullOrWhiteSpace(phone));

    private static string DestinationCity(OrderSummaryData data) =>
        Display(string.IsNullOrWhiteSpace(data.CustomerCity) ? data.CustomerAddress : data.CustomerCity);

    private static string Amount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
    private static string Display(string value) => string.IsNullOrWhiteSpace(value) ? "غير متاح" : value;
    private static string Month(int month) => month is >= 1 and <= 12 ? ArabicMonths[month - 1] : "شهر غير محدد";
}
