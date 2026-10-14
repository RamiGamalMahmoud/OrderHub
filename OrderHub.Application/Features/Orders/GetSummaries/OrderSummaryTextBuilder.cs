using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OrderHub.Domain.Services.Pricing;

namespace OrderHub.Application.Features.Orders.GetSummaries;

/// <summary>Builds copyable Arabic order summaries from persisted order and work-record data.</summary>
public static class OrderSummaryTextBuilder
{
    private static readonly string[] ArabicMonths =
    ["يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"];

    public static IReadOnlyList<OrderSummaryText> Build(OrderSummaryData data)
    {
        var summaries = new List<OrderSummaryText>
        {
            new("order", 0, "الطلب", $"طلب رقم {data.OrderNumber}", BuildCommon(data, includePrices: false)),
            new("customer", 0, "العميل", $"طلب رقم {data.OrderNumber}", BuildCommon(data, includePrices: true))
        };
        summaries[0] = summaries[0] with { Text = AddTitle(summaries[0].Title, summaries[0].Text) };
        summaries[1] = summaries[1] with { Text = AddTitle(summaries[1].Title, summaries[1].Text) };
        foreach (var record in data.DeliveryRecords)
        {
            var title = $"{record.Kind} {record.Number} — {Month(record.Month)} {record.Year}";
            var audience = record.Kind == "مشوار" ? "deliveryman" : "shipping";
            var text = AddTitle(title, BuildCommon(data, false));
            text += $"\nجهة التنفيذ: {record.PartyName}";
            if (record.StepOrder.HasValue)
                text += $"\nخطوة التوصيل: {record.StepOrder}\nمن: {Display(record.From)}\nإلى: {Display(record.To)}";
            else text += $"\nالتسليم إلى: العميل";
            text += "\n\nالمنتجات:\n" + Lines(data.Items, false);
            summaries.Add(new(audience, record.PartyId, record.PartyName, title, text));
        }
        foreach (var record in data.SupplierRecords)
        {
            var title = $"توريد {record.Number} — {Month(record.Month)} {record.Year}";
            var text = AddTitle(title, BuildHeader(data));
            text += $"\nالمورد: {record.SupplierName}\n\nالمنتجات:\n{Lines(record.Items, false)}";
            text += $"\nإجمالي الفاتورة: {data.InvoiceTotal.ToString("N2", CultureInfo.CurrentCulture)} ر.س";
            summaries.Add(new("supplier", record.SupplierId, record.SupplierName, title, text));
        }
        return summaries;
    }

    private static string BuildCommon(OrderSummaryData data, bool includePrices) =>
        BuildHeader(data) + "\n\nالمنتجات:\n" + Lines(data.Items, includePrices) +
        $"\nإجمالي الفاتورة: {data.InvoiceTotal.ToString("N2", CultureInfo.CurrentCulture)} ر.س";

    private static string BuildHeader(OrderSummaryData data) =>
        $"رقم الطلب: {data.OrderNumber}\nتاريخ الطلب: {data.OrderDate.ToLocalTime():dd/MM/yyyy}\n" +
        $"العميل: {Display(data.CustomerName)}\nالهاتف: {Display(data.CustomerPhone)}\n" +
        $"العنوان: {Display(data.CustomerAddress)}\nالموقع: {Display(data.CustomerLocation)}";

    private static string Lines(IEnumerable<OrderSummaryLine> items, bool includePrices) => string.Join("\n", items.Select(item =>
    {
        if (!includePrices) return $"• {item.ProductName} — الكمية: {item.Quantity}";
        var pricing = PricingCalculator.CalculateItem(new PricingItem(0, item.ProductName, item.Quantity, item.UnitPrice, item.VatRate));
        return $"• {item.ProductName} — الكمية: {item.Quantity} — سعر الوحدة قبل الضريبة: {item.UnitPrice.ToString("N2", CultureInfo.CurrentCulture)} ر.س — الضريبة: {item.VatRate}% ({pricing.VatAmount.ToString("N2", CultureInfo.CurrentCulture)} ر.س) — الإجمالي شامل الضريبة: {pricing.Total.ToString("N2", CultureInfo.CurrentCulture)} ر.س";
    }));

    private static string AddTitle(string title, string body) => title + "\n\n" + body;
    private static string Display(string value) => string.IsNullOrWhiteSpace(value) ? "غير متاح" : value;
    private static string Month(int month) => month is >= 1 and <= 12 ? ArabicMonths[month - 1] : "شهر غير محدد";
}
