using OrderHub.Application.Features.Orders.GetSummaries;
using OrderHub.Domain.Models;
using System.Linq;

namespace OrderHub.Tests;

public sealed class OrderSummaryTextBuilderTests
{
    [Fact]
    public void Customer_message_uses_the_dispatch_template_with_tax_inclusive_prices()
    {
        var audiences = OrderSummaryTextBuilder.Build(Sample());

        var customer = Assert.Single(audiences, audience => audience.AudienceType == "customer");
        Assert.Equal("العميل", customer.RoleLabel);
        Assert.Equal("+966500000000", customer.Phone);
        Assert.Equal("الرياض - شارع 1", customer.Destination);
        Assert.Equal("عند الاستلام", customer.CollectionLabel);
        Assert.Equal(165m, customer.CollectionAmount);

        var message = Assert.Single(customer.Records);
        Assert.Equal("طلب رقم ORD-00123", message.Title);
        Assert.Contains("مرحباً بك يا العميل", message.Text);
        Assert.Contains("*ORD-00123*", message.Text);
        Assert.Contains("منتج أ – الكمية: 2 (السعر: 165.00 ر.س)", message.Text);
        Assert.Contains("إجمالي الفاتورة شامل الضريبة:* 165.00 ر.س", message.Text);
        Assert.Contains("عنوان التسليم:* الرياض - شارع 1", message.Text);
        Assert.DoesNotContain("سعر الوحدة", message.Text);
        Assert.DoesNotContain("امر توريد", message.Text);
    }

    [Fact]
    public void Courier_trips_group_per_driver_and_keep_collection_details()
    {
        var data = Sample() with
        {
            DeliveryRecords =
            [
                new("مشوار", 31, 7, 2026, 2, "مندوب 1", 1, "شركة الشحن", "مندوب 1", "0551234567"),
                new("مشوار", 31, 8, 2026, 2, "مندوب 1", 2, "مندوب 1", "العميل", "0551234567")
            ]
        };

        var audiences = OrderSummaryTextBuilder.Build(data);

        var courier = Assert.Single(audiences, audience => audience.AudienceType == "deliveryman");
        Assert.Equal("مندوب التوصيل", courier.RoleLabel);
        Assert.Equal("0551234567", courier.Phone);
        Assert.Equal(165m, courier.CollectionAmount);
        Assert.Equal(
            ["مشوار 7 — فبراير 2026", "مشوار 8 — فبراير 2026"],
            courier.Records.Select(record => record.Title));

        var first = courier.Records[0];
        Assert.Contains("*المسار:* شركة الشحن ← مندوب 1", first.Text);
        Assert.Contains("رقم الجوال: +966500000000", first.Text);
        Assert.Contains("المبلغ المطلوب تحصيله نقداً", first.Text);
        Assert.Contains("*165.00 ر.س*", first.Text);
        Assert.Contains("منتج أ (الكمية: 2)", first.Text);
        Assert.DoesNotContain("السعر:", first.Text);
    }

    [Fact]
    public void Non_cash_orders_do_not_ask_the_courier_for_collection()
    {
        var data = Sample() with { PaymentMethodCode = "MADA", PaymentMethodName = "مدى" };

        var courier = Assert.Single(OrderSummaryTextBuilder.Build(data), audience => audience.AudienceType == "deliveryman");

        Assert.Equal("مدى", courier.CollectionLabel);
        Assert.Null(courier.CollectionAmount);
        Assert.Contains("طريقة الدفع:* مدى — لا يوجد تحصيل نقدي.", courier.Records[0].Text);
    }

    [Fact]
    public void Supplier_message_contains_only_its_own_items_and_no_customer_data()
    {
        var data = Sample() with
        {
            SupplierRecords =
            [
                new(11, 4, 2026, 1, "المورد أ", [new("منتج المورد", 2, 75m)], "0112345678", "الرياض - المصنع")
            ]
        };

        var supplier = Assert.Single(OrderSummaryTextBuilder.Build(data), audience => audience.AudienceType == "supplier");
        Assert.Equal("المورد", supplier.RoleLabel);
        Assert.Equal("0112345678", supplier.Phone);
        Assert.Equal("الرياض - المصنع", supplier.Destination);
        Assert.Null(supplier.CollectionAmount);

        var message = Assert.Single(supplier.Records);
        Assert.Equal("توريد 4 — يناير 2026", message.Title);
        Assert.Contains("أمر صرف توريد 4 — يناير 2026", message.Text);
        Assert.Contains("مرتبط بالطلب: *ORD-00123*", message.Text);
        Assert.Contains("الكمية المطلوبة:* 2 قطعة", message.Text);
        Assert.DoesNotContain("منتج أ", message.Text);
        Assert.DoesNotContain("منتج آخر", message.Text);
        Assert.DoesNotContain("966500000000", message.Text);
        Assert.DoesNotContain("إجمالي الفاتورة", message.Text);
    }

    [Fact]
    public void Carrier_message_uses_the_customer_city_and_cod_flag()
    {
        var data = Sample() with
        {
            DeliveryRecords = [new("شحنة", 47, 8, 2026, 2, "شركة الشحن", null, null, null, "0111111111")]
        };

        var carrier = Assert.Single(OrderSummaryTextBuilder.Build(data), audience => audience.AudienceType == "shipping");
        Assert.Equal("شركة الشحن", carrier.RoleLabel);
        Assert.Equal("0111111111", carrier.Phone);
        Assert.Equal("الرياض", carrier.Destination);

        var message = Assert.Single(carrier.Records);
        Assert.Equal("طلب رقم ORD-00123", message.Title);
        Assert.Contains("*رقم المرجع:* ORD-00123", message.Text);
        Assert.Contains("*المدينة:* الرياض", message.Text);
        Assert.Contains("شحن مع تحصيل COD:* نعم (165.00 ر.س)", message.Text);
        Assert.DoesNotContain("مشوار", message.Text);
    }

    [Fact]
    public void Missing_optional_values_render_placeholders_instead_of_empty_fields()
    {
        var data = Sample() with
        {
            CustomerName = " ",
            CustomerPhone = null,
            CustomerAddress = null
        };

        var customer = Assert.Single(OrderSummaryTextBuilder.Build(data), audience => audience.AudienceType == "customer");

        Assert.Equal("غير متاح", customer.Name);
        Assert.Null(customer.Phone);
        Assert.Equal("غير متاح", customer.Destination);
        Assert.Contains("عنوان التسليم:* غير متاح", customer.Records[0].Text);
    }

    [Fact]
    public void Order_collects_one_supply_work_record_per_supplier()
    {
        var order = new Order(1, "ORD-1");
        Assert.True(order.AddOrderItem(1, "أ", 10, 1, 0, "مورد 1", 11, []).IsSuccess);
        Assert.True(order.AddOrderItem(2, "ب", 20, 1, 0, "مورد 1", 11, []).IsSuccess);
        Assert.True(order.AddOrderItem(3, "ج", 30, 1, 0, "مورد 2", 12, []).IsSuccess);

        Assert.Equal([11, 12], order.Supplies.Select(supply => supply.SupplierId).Order());
        Assert.All(order.Supplies, supply => Assert.Equal(0, supply.WorkRecordNumber));
    }

    private static OrderSummaryData Sample() => new(
        "ORD-00123", new(2026, 1, 15),
        "العميل", "+966500000000", "الرياض - شارع 1", "رابط الموقع", "الرياض",
        165m, "CASH", "نقداً عند الاستلام",
        [new("منتج أ", 2, 75m, 10m), new("منتج آخر", 1, 0m, 0m)],
        [new("مشوار", 31, 7, 2026, 2, "مندوب 1", 1, "شركة الشحن", "مندوب 1", "0551234567")],
        []);
}
