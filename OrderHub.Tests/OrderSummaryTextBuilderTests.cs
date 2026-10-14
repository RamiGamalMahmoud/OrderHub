using OrderHub.Application.Features.Orders.GetSummaries;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Tests;

public sealed class OrderSummaryTextBuilderTests
{
    [Fact]
    public void BuildsOrderAndCustomerSummariesWithSharedHeaderAndDifferentLineDetails()
    {
        var data = Sample();
        var summaries = OrderSummaryTextBuilder.Build(data);

        Assert.Equal("طلب رقم ORD-00123", summaries[0].Title);
        Assert.Equal(summaries[0].Text.Split("\n\n")[1].Split("\n\n")[0],
            summaries[1].Text.Split("\n\n")[1].Split("\n\n")[0]);
        Assert.Contains("تاريخ الطلب:", summaries[0].Text);
        Assert.Contains("الهاتف:", summaries[0].Text);
        Assert.Contains("الموقع: رابط الموقع", summaries[0].Text);
        Assert.Contains("الكمية: 2", summaries[0].Text);
        Assert.DoesNotContain("سعر الوحدة", summaries[0].Text);
        Assert.Contains("سعر الوحدة", summaries[1].Text);
        Assert.Contains("إجمالي الفاتورة: 150.00 ر.س", summaries[0].Text);
        Assert.Contains("إجمالي الفاتورة: 150.00 ر.س", summaries[1].Text);
    }

    [Fact]
    public void PartySummariesUsePersistedPeriodsAndHideItemPrices()
    {
        var data = Sample() with
        {
            DeliveryRecords = [new("مشوار", 31, 7, 2026, 1, "مندوب 1", 2, "شركة الشحن", "مندوب 2")],
            SupplierRecords = [new(11, 4, 2026, 1, "المورد أ", [new("منتج المورد", 2, 75)])]
        };

        var summaries = OrderSummaryTextBuilder.Build(data);
        var trip = summaries.Single(summary => summary.Title.StartsWith("مشوار"));
        var supply = summaries.Single(summary => summary.Title.StartsWith("توريد"));

        Assert.Equal("مشوار 7 — يناير 2026", trip.Title);
        Assert.Contains("من: شركة الشحن", trip.Text);
        Assert.Contains("إلى: مندوب 2", trip.Text);
        Assert.DoesNotContain("سعر الوحدة", trip.Text);
        Assert.Equal("توريد 4 — يناير 2026", supply.Title);
        Assert.Contains("منتج المورد", supply.Text);
        Assert.DoesNotContain("منتج آخر", supply.Text);
        Assert.DoesNotContain("سعر الوحدة", supply.Text);
        Assert.Contains("إجمالي الفاتورة: 150.00 ر.س", supply.Text);
    }

    [Fact]
    public void Each_delivery_attempt_gets_a_separate_summary_and_shipments_hide_prices()
    {
        var data = Sample() with
        {
            DeliveryRecords =
            [
                new("مشوار", 31, 2, 2026, 1, "مندوب", 1, null, "شركة الشحن"),
                new("مشوار", 31, 3, 2026, 1, "مندوب", 1, null, "شركة الشحن"),
                new("شحنة", 47, 8, 2026, 1, "شركة الشحن", 2, "مندوب", "العميل")
            ]
        };

        var summaries = OrderSummaryTextBuilder.Build(data);
        var workSummaries = summaries.Where(summary => summary.Title.StartsWith("مشوار") || summary.Title.StartsWith("شحنة")).ToList();

        Assert.Equal(3, workSummaries.Count);
        Assert.Contains(workSummaries, summary => summary.Title == "مشوار 2 — يناير 2026");
        Assert.Contains(workSummaries, summary => summary.Title == "مشوار 3 — يناير 2026");
        var shipment = Assert.Single(workSummaries, summary => summary.Title.StartsWith("شحنة"));
        Assert.Contains("الكمية: 2", shipment.Text);
        Assert.DoesNotContain("سعر الوحدة", shipment.Text);
    }

    [Fact]
    public void MissingOptionalLocationAndDeliveryEndpointsAreHandledWithoutAllocatingNumbers()
    {
        var data = Sample() with
        {
            CustomerLocation = null,
            DeliveryRecords = [new("شحنة", 47, 3, 2026, 2, "شركة الشحن", 1, null, null)]
        };

        var summaries = OrderSummaryTextBuilder.Build(data);
        var shipment = summaries.Single(summary => summary.Title.StartsWith("شحنة"));

        Assert.Contains("الموقع: غير متاح", shipment.Text);
        Assert.Contains("من: غير متاح", shipment.Text);
        Assert.Contains("إلى: غير متاح", shipment.Text);
        Assert.Equal(3, data.DeliveryRecords[0].Number);
    }

    [Fact]
    public void Order_collects_one_supply_work_record_per_supplier()
    {
        var order = new Order(1, "ORD-1");
        Assert.True(order.AddOrderItem(1, "أ", 10, 1, 0, "مورد 1", 11, []).IsSuccess);
        Assert.True(order.AddOrderItem(2, "ب", 20, 1, 0, "مورد 1", 11, []).IsSuccess);
        Assert.True(order.AddOrderItem(3, "ج", 30, 1, 0, "مورد 2", 12, []).IsSuccess);

        Assert.Equal(new[] { 11, 12 }, order.Supplies.Select(supply => supply.SupplierId).Order());
        Assert.All(order.Supplies, supply => Assert.Equal(0, supply.WorkRecordNumber));
    }

    private static OrderSummaryData Sample() => new(
        "ORD-00123", new(2026, 1, 15), "العميل", "+966500000000", "الرياض - شارع 1", "رابط الموقع", 150m,
        [new("منتج أ", 2, 75m), new("منتج آخر", 1, 0m)],
        new List<PartySummaryRecord>(), new List<SupplierSummaryRecord>());
}
