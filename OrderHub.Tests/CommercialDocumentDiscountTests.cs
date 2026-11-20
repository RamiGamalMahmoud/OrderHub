using Microsoft.EntityFrameworkCore;
using OrderHub.Application.DTOs.Documents;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.Documents;
using System.Text;
using QuestPDF.Infrastructure;

namespace OrderHub.Tests;

public sealed class CommercialDocumentDiscountTests
{
    [Fact]
    public async Task Persisted_invoice_financial_snapshot_does_not_change_when_order_is_edited()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (client, product) = await SeedOrderParentsAsync(context);

        var order = new Order(client.Id, "ORD-SNAPSHOT");
        Assert.True(order.AddOrderItem(product.Id, "Product", 400m, 1, 5m, "", null, []).IsSuccess);
        Assert.True(order.SetFinancialAdjustments(15m, DiscountType.Percentage, 10m).IsSuccess);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var invoice = Invoice.Create("INV-TEST", DateTime.UtcNow, "Client", "123", "Address",
            order.Pricing.Subtotal, order.Pricing.TotalVat, order.Total.Value, order.Id,
            order.DeliveryCost, order.DiscountType, order.DiscountValue, order.DiscountAmount);
        var quotation = Quotation.Create("QT-TEST", DateTime.UtcNow, "Client", "123", "Address",
            order.Pricing.Subtotal, order.Pricing.TotalVat, order.Total.Value, DateTime.UtcNow.AddDays(30),
            Guid.NewGuid(), order.DeliveryCost, order.DiscountType, order.DiscountValue, order.DiscountAmount);
        var proforma = ProformaInvoice.Create("PI-TEST", DateTime.UtcNow, "Client", "123", "Address",
            order.Pricing.Subtotal, order.Pricing.TotalVat, order.Total.Value, Guid.NewGuid(),
            order.DeliveryCost, order.DiscountType, order.DiscountValue, order.DiscountAmount);
        context.Invoices.Add(invoice);
        context.Quotations.Add(quotation);
        context.ProformaInvoices.Add(proforma);
        await context.SaveChangesAsync();

        Assert.True(order.SetFinancialAdjustments(50m, DiscountType.FixedAmount, 25m).IsSuccess);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var savedInvoice = await context.Invoices.SingleAsync();
        var savedQuotation = await context.Quotations.SingleAsync();
        var savedProformaInvoice = await context.ProformaInvoices.SingleAsync();

        AssertFinancialSnapshot(savedInvoice);
        AssertFinancialSnapshot(savedQuotation);
        AssertFinancialSnapshot(savedProformaInvoice);
    }

    [Fact]
    public async Task Order_financial_columns_have_required_defaults_and_preserve_existing_totals()
    {
        PostgresTestDatabase.Reset();
        await using var context = new AppDbContext(PostgresTestDatabase.Options);
        var (client, product) = await SeedOrderParentsAsync(context);

        var entity = context.Model.FindEntityType(typeof(Order))!;
        foreach (var name in new[] { nameof(Order.DeliveryCost), nameof(Order.DiscountValue), nameof(Order.DiscountAmount) })
        {
            var property = entity.FindProperty(name)!;
            Assert.False(property.IsNullable);
            Assert.Equal("numeric(18,2)", property.GetColumnType());
            Assert.Equal(0m, property.GetDefaultValue());
        }
        var type = entity.FindProperty(nameof(Order.DiscountType))!;
        Assert.False(type.IsNullable);
        Assert.Equal(DiscountType.FixedAmount, Assert.IsType<DiscountType>(type.GetDefaultValue()));
        Assert.Equal("varchar(30)", type.GetColumnType());

        var historical = new Order(client.Id, "ORD-DEFAULT");
        Assert.True(historical.AddOrderItem(product.Id, "Product", 400m, 1, 5m, "", null, []).IsSuccess);
        context.Orders.Add(historical);
        await context.SaveChangesAsync();
        Assert.Equal(420m, historical.Total.Value);
        context.ChangeTracker.Clear();
        var reopened = await context.Orders.Include(order => order.OrderItems).SingleAsync();
        Assert.Equal(0m, reopened.DeliveryCost);
        Assert.Equal(DiscountType.FixedAmount, reopened.DiscountType);
        Assert.Equal(0m, reopened.DiscountValue);
        Assert.Equal(0m, reopened.DiscountAmount);
        Assert.Equal(420m, reopened.Total.Value);
    }

    [Fact]
    public void Invoice_quotation_and_proforma_pdf_render_the_financial_summary()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        var previousDirectory = Environment.CurrentDirectory;
        Environment.CurrentDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../OrderHub.Web"));
        var customer = new DocumentCustomer("Client", "123", "Address");
        var product = new InvoicePdfDataItem(1, "Product", 1, 400m, 5m, 400m, 20m, 420m);
        var invoice = new InvoicePdfData("INV-TEST", 1, "ORD-1", "30-10-2026", customer,
            400m, 20m, 395m, [product], 15m, DiscountType.Percentage, 10m, 40m);
        var quotation = new QuotationPdfData("QT-TEST", Guid.NewGuid(), null, null, "30-10-2026", customer,
            400m, 20m, 395m, [new QuotationPdfDataItem(1, "Product", 1, 400m, 5m, 400m, 20m, 420m)],
            15m, DiscountType.Percentage, 10m, 40m);
        var proforma = new ProformaInvoicePdfData("PI-TEST", Guid.NewGuid(), null, null, "30-10-2026", customer,
            400m, 20m, 395m, [new ProformaInvoicePdfDataDataItem(1, "Product", 1, 400m, 5m, 400m, 20m, 420m)],
            15m, DiscountType.Percentage, 10m, 40m);

        try
        {
            AssertFinancialPdf(new InvoicePdfDocument(invoice).Build());
            AssertFinancialPdf(new QuotationDocument(quotation).Build());
            AssertFinancialPdf(new ProformaInvoiceDocument(proforma).Build());
        }
        finally
        {
            Environment.CurrentDirectory = previousDirectory;
        }
    }

    private static void AssertFinancialPdf(byte[] pdf)
    {
        Assert.True(pdf.Length > 1000);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    private static async Task<(Client Client, Product Product)> SeedOrderParentsAsync(AppDbContext context)
    {
        var city = new City("الرياض");
        var client = new Client("عميل", "شارع الملك", city, "0501111111", "+966");
        var category = new Category("مستلزمات");
        var product = new Product("منتج", "PRD-001", category, new Money(400m));
        context.Cities.Add(city);
        context.Clients.Add(client);
        context.Categories.Add(category);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return (client, product);
    }

    private static void AssertFinancialSnapshot<TItem>(CommercialDocument<TItem> snapshot)
        where TItem : CommercialDocumentItem
    {
        Assert.Equal(400m, snapshot.Subtotal);
        Assert.Equal(20m, snapshot.TotalVat);
        Assert.Equal(420m, snapshot.Subtotal + snapshot.TotalVat);
        Assert.Equal(40m, snapshot.DiscountAmount);
        Assert.Equal(15m, snapshot.DeliveryCost);
        Assert.Equal(395m, snapshot.TotalAmount);
    }
}
