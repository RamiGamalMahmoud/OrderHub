using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Microsoft.AspNetCore.Components;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Features.Setup.Properties.Get;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Pages.Features.Orders.Editor;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.OrderDetails;

namespace OrderHub.Tests.Web;

public class DraftRestorationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(9)]
    [InlineData(99)]
    public async Task Saved_values_and_optional_supplier_are_preserved(int? supplierId)
    {
        var state = new OrderEditorState
        {
            ClientId = 5, ClientName = "Saved client", OrderSource = OrderSource.Online, PaymentMethodId = 41,
            Delivery = new() { Method = DeliveryMethod.DeliveryMan, DeliverymanId = 31, ShippingCarrierId = 32 },
            Items = [new()
            {
                ProductId = 7, ProductName = "Saved product", CategoryName = "Saved category",
                UnitPrice = 12.34m, Quantity = 3, VatRate = 14.75m, SupplierId = supplierId,
                Properties = [new() { PropertyId = 21, Name = "List", IsRequired = true, Type = PropertyType.List,
                    Value = "Red", Options = [new(1, "Red")] }]
            }]
        };
        var restored = await RoundTrip(state, Product());
        Assert.Equal(5, restored.ClientId);
        Assert.Equal("Saved client", restored.ClientName);
        Assert.Equal(OrderSource.Online, restored.OrderSource);
        Assert.Equal(41, restored.PaymentMethodId);
        Assert.Equal(DeliveryMethod.DeliveryMan, restored.Delivery.Method);
        Assert.Equal(31, restored.Delivery.DeliverymanId);
        Assert.Equal(32, restored.Delivery.ShippingCarrierId);
        var item = Assert.Single(restored.Items);
        Assert.Equal(7, item.ProductId);
        Assert.Equal("Saved product", item.ProductName);
        Assert.Equal("Saved category", item.CategoryName);
        Assert.Equal(12.34m, item.UnitPrice);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(14.75m, item.VatRate);
        Assert.Equal(supplierId, item.SupplierId);
        Assert.Equal(new OrderItemSupplierEditorState(9, "Current supplier"), Assert.Single(item.Suppliers));
        Assert.Equal("Red", item.Properties.Single(p => p.PropertyId == 21).Value);
    }

    [Theory]
    [InlineData(PropertyType.List, true, null)]
    [InlineData(PropertyType.List, true, "Red")]
    [InlineData(PropertyType.List, true, "Removed option")]
    [InlineData(PropertyType.Boolean, true, "false")]
    [InlineData(PropertyType.Boolean, true, "invalid")]
    [InlineData(PropertyType.Text, true, "   ")]
    [InlineData(PropertyType.Text, false, null)]
    public async Task Property_validation_semantics_survive_save_and_load(PropertyType type, bool required, string value)
    {
        var property = new OrderItemPropertyEditorState
        {
            PropertyId = 21, Name = "Current field", Type = type, IsRequired = required,
            Value = value, Options = [new(1, "Red")]
        };
        var state = State(property);
        var metadata = Product(new GetOrderItemEditor.OrderItemProperty(21, "Current field", required, type, [new(1, "Red")]));
        var restored = (await RoundTrip(state, metadata)).Items.Single().Properties.Single();
        Assert.Equal(property.ValidateValue(value), restored.ValidateValue(restored.Value));
        Assert.Equal(property.Name, restored.Name);
        Assert.Equal(property.Type, restored.Type);
        Assert.Equal(property.IsRequired, restored.IsRequired);
        Assert.Equal(property.Value, restored.Value);
        Assert.Equal(property.Options, restored.Options);
    }

    [Fact]
    public async Task New_definitions_and_surviving_historical_metadata_are_merged_without_losing_values()
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<GetPropertyQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertyDetailsDto(22, "Historical boolean", PropertyType.Boolean, null, []));
        var state = State(new() { PropertyId = 21, Name = "List", Value = "Red" },
            new() { PropertyId = 22, Name = "Old", Value = "false" });
        var restored = await RoundTrip(state, Product(), executor);
        var properties = restored.Items.Single().Properties;
        Assert.Equal(3, properties.Count);
        var added = properties.Single(p => p.PropertyId == 23);
        Assert.Null(added.Value);
        Assert.True(added.IsRequired);
        Assert.NotNull(added.ValidateValue(null));
        var historical = properties.Single(p => p.PropertyId == 22);
        Assert.Equal("false", historical.Value);
        Assert.Equal("Historical boolean", historical.Name);
        Assert.Equal(PropertyType.Boolean, historical.Type);
        Assert.False(historical.IsRequired);
        Assert.True(historical.HasMetadata);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(DeliveryMethod.DeliveryChain)]
    public async Task Incomplete_order_and_chain_handlers_load_without_forcing_values(DeliveryMethod? method)
    {
        var state = new OrderEditorState
        {
            Items = State(new OrderItemPropertyEditorState { PropertyId = 21, Name = "List", Value = null }).Items,
            Delivery = new()
            {
                Method = method,
                Steps = [new() { StepOrder = 2, Method = DeliveryMethod.ShippingCompany, HandlerId = null },
                    new() { StepOrder = 1, Method = DeliveryMethod.DeliveryMan, HandlerId = 51 }]
            }
        };
        var restored = await RoundTrip(state, Product());
        Assert.Null(restored.ClientId);
        Assert.Null(restored.OrderSource);
        Assert.Null(restored.PaymentMethodId);
        Assert.Equal(method, restored.Delivery.Method);
        Assert.Null(restored.Delivery.DeliverymanId);
        Assert.Null(restored.Delivery.ShippingCarrierId);
        Assert.Equal(new[] { 1, 2 }, restored.Delivery.Steps.Select(s => s.StepOrder));
        Assert.Equal(new int?[] { 51, null }, restored.Delivery.Steps.Select(s => s.HandlerId));
        Assert.Equal(new[] { DeliveryMethod.DeliveryMan, DeliveryMethod.ShippingCompany }, restored.Delivery.Steps.Select(s => s.Method));
        Assert.Null(restored.Items.Single().SupplierId);
        Assert.Null(restored.Items.Single().Properties.Single(p => p.PropertyId == 21).Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_or_failed_product_metadata_preserves_the_item_and_reports_the_limitation(bool queryFails)
    {
        var executor = new Mock<IRequestExecutor>();
        if (queryFails)
            executor.Setup(x => x.ExecuteAsync(It.IsAny<GetOrderItemEditor.Query>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Catalog unavailable"));
        var restored = await RoundTrip(State(new OrderItemPropertyEditorState { PropertyId = 77, Name = "Previous", Value = "Saved" }), null, executor, queryFails);
        var item = Assert.Single(restored.Items);
        Assert.Equal("Saved", Assert.Single(item.Properties).Value);
        Assert.NotNull(item.MetadataRestorationError);
        Assert.False(item.Properties.Single().HasMetadata);
        Assert.False(item.CreateWorkingCopy().Properties.Single().HasMetadata);
        Assert.NotNull(item.Properties.Single().ValidateValue("Saved"));
        Assert.NotEmpty(Validation(restored));
        restored.Items.Clear();
        Assert.DoesNotContain(Validation(restored), e => e.Contains(item.MetadataRestorationError));
    }

    [Fact]
    public async Task Missing_historical_definition_keeps_value_but_does_not_claim_valid_metadata()
    {
        var restored = await RoundTrip(State(new OrderItemPropertyEditorState { PropertyId = 77, Name = "Old", Value = "Saved" }), Product());
        var historical = restored.Items.Single().Properties.Single(p => p.PropertyId == 77);
        Assert.Equal("Saved", historical.Value);
        Assert.False(historical.HasMetadata);
        Assert.NotNull(historical.ValidateValue(historical.Value));
    }

    [Fact]
    public async Task Client_selection_records_the_available_display_name_and_clearing_removes_it()
    {
        var state = new OrderEditorState();
        var changes = 0;
        using var details = new DetailsStep
        {
            State = state,
            DetailsChanged = EventCallback.Factory.Create(new object(), () => changes++)
        };
        typeof(DetailsStep).GetField("_clients", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(details, new ClientLookup[] { new(5, "Available client", "Phone", "Address") });
        var select = typeof(DetailsStep).GetMethod("SetClientAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        await (Task)select.Invoke(details, new object[] { (int?)5 });
        Assert.Equal("Available client", state.ClientName);
        await (Task)select.Invoke(details, new object[] { null });
        Assert.Null(state.ClientName);
        Assert.Equal(2, changes);
    }

    private static OrderEditorState State(params OrderItemPropertyEditorState[] properties) => new()
    {
        Items = [new() { ProductId = 7, ProductName = "Saved product", CategoryName = "Saved category",
            UnitPrice = 12.34m, Quantity = 3, VatRate = 14.75m, Properties = properties.ToList() }]
    };

    private static GetOrderItemEditor.OrderItemEditorDto Product(params GetOrderItemEditor.OrderItemProperty[] definitions) => new(
        7, 999m, "Current product", "Current category", [new(9, "Current supplier")],
        definitions.Length > 0 ? definitions : [new(21, "List", true, PropertyType.List, [new(1, "Red")]),
            new(23, "New required field", true, PropertyType.Text, [])]);

    private static async Task<OrderEditorState> RoundTrip(OrderEditorState state, GetOrderItemEditor.OrderItemEditorDto metadata,
        Mock<IRequestExecutor> executor = null, bool queryFails = false)
    {
        executor ??= new Mock<IRequestExecutor>();
        if (!queryFails)
            executor.Setup(x => x.ExecuteAsync(It.IsAny<GetOrderItemEditor.Query>(), It.IsAny<CancellationToken>())).ReturnsAsync(metadata);
        var user = new Mock<ICurrentUser>();
        user.Setup(x => x.GetUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(11);
        Draft saved = null;
        var store = new Mock<IDraftStore>();
        store.Setup(x => x.SaveAsync(It.IsAny<Draft>())).Callback<Draft>(draft =>
            saved = JsonSerializer.Deserialize<Draft>(JsonSerializer.Serialize(draft))).Returns(Task.CompletedTask);
        store.Setup(x => x.GetAsync(It.IsAny<Guid>(), 11)).ReturnsAsync(() => saved);
        var editor = new OrderEditor();
        SetService(editor, "CurrentUser", user.Object);
        SetService(editor, "DraftStore", store.Object);
        SetService(editor, "RequestExecutor", executor.Object);
        typeof(OrderEditor).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(editor, state);
        typeof(OrderEditor).GetField("_userId", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(editor, 11);
        await (Task)Method("SaveDraft").Invoke(editor, null);
        return await (Task<OrderEditorState>)Method("LoadDraftAsync").Invoke(editor, new object[] { saved.Id });
    }

    private static string[] Validation(OrderEditorState state) =>
        ((System.Collections.Generic.IReadOnlyList<string>)typeof(OrderEditor)
            .GetMethod("GetValidationErrors", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { state })).ToArray();

    private static MethodInfo Method(string name) => typeof(OrderEditor).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
    private static void SetService(OrderEditor editor, string name, object value) =>
        typeof(OrderEditor).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(editor, value);
}
