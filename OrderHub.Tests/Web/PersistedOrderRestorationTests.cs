using OrderHub.Application.Features.Orders.GetForEdit;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;

using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure.Features.Orders;
using Microsoft.EntityFrameworkCore;
using OrderHub.Tests.Infrastructure;
using OrderHub.Web.Components.Pages.Features.Orders.Editor;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Tests.Web;

public class PersistedOrderRestorationTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(9, false)]
    [InlineData(99, true)]
    public async Task Persisted_values_and_current_metadata_reach_editor(int? supplierId, bool legacySource)
    {
        var product = new Product("Current name", "P7", new Category("Current category"), new Money(500));
        var supplier = new Supplier("Current supplier");
        Set(supplier, nameof(ModelBase.Id), 9);
        product.AddSupplier(supplier);
        var definition = Property.Create("Current list", PropertyType.List, options: ["Saved", "Other"]);
        Set(definition, nameof(ModelBase.Id), 21);
        product.AddProperty(21, true);
        product.Properties.Single().Property = definition;
        var historical = Property.Create("Removed boolean", PropertyType.Boolean);
        Set(historical, nameof(ModelBase.Id), 22);
        var order = new Order(5, "ORD-RESTORE");
        order.UpdateHeader(5, OrderSource.Online, DeliveryMethod.DeliveryMan, 31, null, 41);
        if (legacySource)
            Set(order, nameof(Order.OrderSource), null);
        var item = order.AddOrderItem(7, "Saved name", 12.34m, 3, 14.75m, "Saved supplier", supplierId,
            [new(21, "Saved"), new(22, "false")]).Value;
        Set(item, nameof(OrderItem.Product), product);
        item.Properties.Single(p => p.PropertyId == 21).Property = definition;
        item.Properties.Single(p => p.PropertyId == 22).Property = historical;

        var dto = await Read(order);
        var state = await Load(dto);

        Assert.Equal(5, state.ClientId);
        Assert.Equal(legacySource ? (OrderSource?)null : OrderSource.Online, state.OrderSource);
        Assert.Equal(41, state.PaymentMethodId);
        Assert.Equal(DeliveryMethod.DeliveryMan, state.Delivery.Method);
        Assert.Equal(31, state.Delivery.DeliverymanId);
        Assert.Null(state.Delivery.ShippingCarrierId);
        var restored = Assert.Single(state.Items);
        Assert.Equal(7, restored.ProductId);
        Assert.Equal("Saved name", restored.ProductName);
        Assert.Equal("Current category", restored.CategoryName);
        Assert.Equal(12.34m, restored.UnitPrice);
        Assert.Equal(3, restored.Quantity);
        Assert.Equal(14.75m, restored.VatRate);
        Assert.Equal(supplierId, restored.SupplierId);
        Assert.Equal(new OrderItemSupplierEditorState(9, "Current supplier"), Assert.Single(restored.Suppliers));
        if (supplierId == 99)
            Assert.DoesNotContain(restored.Suppliers, s => s.Id == restored.SupplierId);
        Assert.Equal(2, restored.Properties.Count);
        var current = restored.Properties.Single(p => p.PropertyId == 21);
        Assert.Equal("Current list", current.Name);
        Assert.True(current.IsRequired);
        Assert.Equal(PropertyType.List, current.Type);
        Assert.Equal("Saved", current.Value);
        Assert.Equal(definition.Options.OrderBy(o => o.Id).Select(o => (o.Id, o.Value)),
            current.Options.OrderBy(o => o.Id).Select(o => (o.Id, o.Value)));
        var removed = restored.Properties.Single(p => p.PropertyId == 22);
        Assert.Equal("Removed boolean", removed.Name);
        Assert.Equal(PropertyType.Boolean, removed.Type);
        Assert.Equal("false", removed.Value);
        Assert.False(removed.IsRequired);
    }

    [Fact]
    public async Task Chain_steps_are_sorted_and_use_only_the_matching_handler()
    {
        var order = new Order(5, "ORD-CHAIN");
        order.UpdateHeader(5, OrderSource.Salla, DeliveryMethod.DeliveryChain, null, null, null);
        order.AddDeliveryStep(new() { StepOrder = 3, DeliveryMethod = DeliveryMethod.DeliveryMan, ShippingCarrierId = 999 });
        order.AddDeliveryStep(new() { StepOrder = 2, DeliveryMethod = DeliveryMethod.ShippingCompany, ShippingCarrierId = 52, DeliverymanId = 999 });
        order.AddDeliveryStep(new() { StepOrder = 1, DeliveryMethod = DeliveryMethod.DeliveryMan, DeliverymanId = 51, ShippingCarrierId = 999 });
        var dto = await Read(order);
        Assert.Equal(new[] { 1, 2, 3 }, dto.DeliverySteps.Select(s => s.StepOrder));
        var state = await Load(dto);
        Assert.Equal(DeliveryMethod.DeliveryChain, state.Delivery.Method);
        Assert.Equal(new[] { 1, 2, 3 }, state.Delivery.Steps.Select(s => s.StepOrder));
        Assert.Equal(new int?[] { 51, 52, null }, state.Delivery.Steps.Select(s => s.HandlerId));
        Assert.Equal(new[] { DeliveryMethod.DeliveryMan, DeliveryMethod.ShippingCompany, DeliveryMethod.DeliveryMan }, state.Delivery.Steps.Select(s => s.Method));
    }

    [Fact]
    public async Task Legacy_null_delivery_and_shipping_selection_are_preserved()
    {
        var legacy = new Order(5, "ORD-LEGACY");
        var state = await Load(await Read(legacy));
        Assert.Null(state.OrderSource);
        Assert.Null(state.Delivery.Method);
        var shipping = new Order(5, "ORD-SHIPPING");
        shipping.UpdateHeader(5, OrderSource.Online, DeliveryMethod.ShippingCompany, null, 77, null);
        state = await Load(await Read(shipping));
        Assert.Equal(DeliveryMethod.ShippingCompany, state.Delivery.Method);
        Assert.Equal(77, state.Delivery.ShippingCarrierId);
        Assert.Null(state.Delivery.DeliverymanId);
    }

    private static async Task<OrderEditorState> Load(OrderEditorData dto)
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<GetOrderForEditQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var editor = new OrderEditor();
        typeof(OrderEditor).GetProperty("RequestExecutor", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, executor.Object);
        return await (Task<OrderEditorState>)typeof(OrderEditor)
            .GetMethod("LoadOrderAsync", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(editor, new object[] { dto.Id });
    }

    private static async Task<OrderEditorData> Read(Order order)
    {
        using var factory = TestDb.CreateFactory();
        using var context = factory.CreateDbContext();
        // These existing restoration cases intentionally model unavailable legacy references.
        // Relax FK enforcement only on this fixture's isolated connection, preserving the schema.
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");
        foreach (var item in order.OrderItems)
            Set(item.Product, nameof(ModelBase.Id), item.ProductId);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return await new OrderReader(factory).GetForEditAsync(order.Id, default);
    }

    private static void Set(object target, string property, object value) =>
        target.GetType().GetProperty(property).SetValue(target, value);
}
