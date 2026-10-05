using OrderHub.Application.Features.Orders.GetForEdit;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using MudBlazor;
using OrderHub.Application.Features.Orders.Contracts;

using OrderHub.Application.Features.Orders.Update;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.Orders;
using OrderHub.Infrastructure.Services;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Web.Components.Pages.Features.Orders.Editor;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Tests.Web;

public class OrderUpdateFlowTests
{
    [Theory]
    [InlineData(DeliveryMethod.Pickup, false)]
    [InlineData(DeliveryMethod.DeliveryMan, true)]
    [InlineData(DeliveryMethod.ShippingCompany, true)]
    [InlineData(DeliveryMethod.DeliveryChain, true)]
    public async Task Unchanged_order_survives_real_read_editor_update_and_database_reload(DeliveryMethod method, bool supplier)
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(method, supplier);
        var before = await fixture.Read(id);
        var editor = await Editor(before);
        var input = Build(editor);
        var result = await fixture.UpdateOrder(id, input);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var after = await fixture.Read(id);
        Assert.Equal(before.ClientId, after.ClientId);
        Assert.Equal(before.OrderSource, after.OrderSource);
        Assert.Equal(before.PaymentMothodId, after.PaymentMothodId);
        Assert.Equal(before.DeliveryMethod, after.DeliveryMethod);
        Assert.Equal(before.DeliveryManId, after.DeliveryManId);
        Assert.Equal(before.ShippingCarrierId, after.ShippingCarrierId);
        var oldItem = Assert.Single(before.OrderItems);
        var newItem = Assert.Single(after.OrderItems);
        Assert.Equal(oldItem.ProductId, newItem.ProductId);
        Assert.Equal(oldItem.ProductName, newItem.ProductName);
        Assert.Equal(oldItem.UnitPrice, newItem.UnitPrice);
        Assert.Equal(oldItem.Quantity, newItem.Quantity);
        Assert.Equal(oldItem.VatRate, newItem.VatRate);
        Assert.Equal(oldItem.SupplierId, newItem.SupplierId);
        Assert.Equal(oldItem.SupplierName, newItem.SupplierName);
        Assert.Equal(oldItem.Properties.Select(p => (p.PropertyId, p.SelectedValue)), newItem.Properties.Select(p => (p.PropertyId, p.SelectedValue)));
        Assert.Equal(before.DeliverySteps, after.DeliverySteps);
    }

    [Fact]
    public async Task Legacy_source_can_be_selected_and_persisted()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.Pickup, false);
        using (var db = fixture.CreateDbContext())
        {
            var order = await db.Orders.SingleAsync();
            Set(order, nameof(Order.OrderSource), null);
            await db.SaveChangesAsync();
        }
        var editor = await Editor(await fixture.Read(id));
        Assert.Null(State(editor).OrderSource);
        State(editor).OrderSource = OrderSource.Salla;
        Assert.True((await fixture.UpdateOrder(id, Build(editor))).IsSuccess);
        Assert.Equal(OrderSource.Salla, (await fixture.Read(id)).OrderSource);
    }

    [Theory]
    [InlineData(DeliveryMethod.Pickup)]
    [InlineData(DeliveryMethod.DeliveryMan)]
    [InlineData(DeliveryMethod.ShippingCompany)]
    [InlineData(DeliveryMethod.DeliveryChain)]
    public async Task Inactive_handlers_and_steps_never_reach_update_persistence(DeliveryMethod method)
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.Pickup, false);
        var editor = await Editor(await fixture.Read(id));
        var state = State(editor);
        state.Delivery.Method = method;
        state.Delivery.DeliverymanId = 31;
        state.Delivery.ShippingCarrierId = 32;
        state.Delivery.Steps = [new() { StepOrder = 8, Method = DeliveryMethod.ShippingCompany, HandlerId = 52 },
            new() { StepOrder = 8, Method = DeliveryMethod.DeliveryMan, HandlerId = 51 }];
        var input = Build(editor);
        Assert.Equal(method == DeliveryMethod.DeliveryMan ? (int?)31 : null, input.DeliveryManId);
        Assert.Equal(method == DeliveryMethod.ShippingCompany ? (int?)32 : null, input.ShippingCarrierId);
        if (method == DeliveryMethod.DeliveryChain)
            Assert.Equal(new[] { 1, 2 }, input.DeliverySteps.Select(s => s.StepOrder));
        else
            Assert.Empty(input.DeliverySteps);
        // Also exercise store-level normalization against a caller supplying stale data.
        input = input with
        {
            DeliveryManId = 31,
            ShippingCarrierId = 32,
            DeliverySteps = input.DeliverySteps.Any() ? input.DeliverySteps : [new(1, DeliveryMethod.DeliveryMan, 51)]
        };
        Assert.True((await fixture.UpdateOrder(id, input)).IsSuccess);
        var after = await fixture.Read(id);
        Assert.Equal(method == DeliveryMethod.DeliveryMan ? (int?)31 : null, after.DeliveryManId);
        Assert.Equal(method == DeliveryMethod.ShippingCompany ? (int?)32 : null, after.ShippingCarrierId);
        if (method == DeliveryMethod.DeliveryChain)
        {
            Assert.Equal(new[] { 1, 2 }, after.DeliverySteps.Select(s => s.StepOrder));
            Assert.Equal(new int?[] { 52, 51 }, after.DeliverySteps.Select(s => s.HandlerId));
        }
        else
            Assert.Empty(after.DeliverySteps);
        Assert.Equal(31, state.Delivery.DeliverymanId); // Mapping does not clear convenience state.
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, 0)]
    public async Task Invalid_replacement_preserves_database_and_tracked_aggregate(decimal price, int quantity)
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.DeliveryChain, true);
        var input = Build(await Editor(await fixture.Read(id)));
        var valid = input.OrderItems.Single();
        input = input with { OrderSource = OrderSource.Salla, OrderItems = [valid, valid with { UnitPrice = price, Quantity = quantity }] };
        using (var db = fixture.CreateDbContext())
        {
            var order = await db.Orders.Include(o => o.OrderItems).ThenInclude(i => i.Properties).Include(o => o.DeliverySteps).SingleAsync();
            var original = order.OrderItems.Single();
            var result = await new UpdateOrderHandler(new OrderRepository(db), new UnitOfWork(db))
                .Handle(new(id, input), default);
            Assert.False(result.IsSuccess);
            Assert.Equal(price == 0 ? "Unit price must be greater than zero." : "Quantity must be greater than zero.", result.ErrorMessage);
            Assert.Same(original, order.OrderItems.Single());
            Assert.Equal(OrderSource.Online, order.OrderSource);
            Assert.Equal(2, order.DeliverySteps.Count);
            Assert.DoesNotContain(db.ChangeTracker.Entries(), e => e.State is EntityState.Deleted or EntityState.Added or EntityState.Modified);
            await db.SaveChangesAsync(); // Even a subsequent save cannot persist partial reconstruction.
        }
        var failure = await fixture.UpdateOrder(id, input);
        Assert.False(failure.IsSuccess);
        Assert.Equal(2, (await fixture.Read(id)).DeliverySteps.Count());
        Assert.Single((await fixture.Read(id)).OrderItems);
    }

    [Fact]
    public async Task Database_failure_rolls_back_header_and_replaced_details()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.DeliveryChain, true);
        var input = Build(await Editor(await fixture.Read(id)));
        // The real mapped product_name column is required. Domain reconstruction permits null.
        input = input with
        {
            OrderSource = OrderSource.Salla,
            OrderItems = [input.OrderItems.Single() with { ProductName = null }]
        };
        Assert.False((await fixture.UpdateOrder(id, input)).IsSuccess);
        var after = await fixture.Read(id);
        Assert.Equal(OrderSource.Online, after.OrderSource);
        Assert.Equal("Saved product", after.OrderItems.Single().ProductName);
        Assert.Equal(2, after.DeliverySteps.Count());
    }

    [Fact]
    public async Task Supplier_snapshot_survives_renaming_and_removed_association_and_new_choice_uses_current_name()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.Pickup, true);
        using (var db = fixture.CreateDbContext())
        {
            var product = await db.Products.Include(p => p.Suppliers).SingleAsync();
            product.ClearSuppliers();
            product.AddSupplier(new Supplier("Replacement supplier"));
            await db.SaveChangesAsync();
        }
        var editor = await Editor(await fixture.Read(id));
        var item = State(editor).Items.Single();
        Assert.DoesNotContain(item.Suppliers, s => s.Id == item.SupplierId);
        var input = Build(editor);
        Assert.Equal("Historical supplier snapshot", input.OrderItems.Single().SupplierName);
        Assert.True((await fixture.UpdateOrder(id, input)).IsSuccess);
        item.SupplierId = item.Suppliers.Single().Id;
        Assert.Equal("Replacement supplier", Build(editor).OrderItems.Single().SupplierName);
        item.SupplierId = 98765;
        Assert.NotEmpty(Errors(State(editor)));
    }

    [Fact]
    public async Task Final_input_is_stable_and_inactive_null_chain_handlers_do_not_block_update()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.Pickup, false);
        var editor = await Editor(await fixture.Read(id));
        var state = State(editor);
        state.Delivery.Steps.Add(new() { Method = DeliveryMethod.DeliveryMan });
        Assert.Empty(Errors(state));
        var input = Build(editor);
        state.Items.Single().Properties.Single().Value = "Changed";
        state.Items.Clear();
        Assert.Equal("Saved", input.OrderItems.Single().Properties.Single().Value);
        Assert.Empty(input.DeliverySteps);
        state.Delivery.Method = DeliveryMethod.DeliveryChain;
        Assert.Contains(Errors(state), e => e.Contains("جهة تنفيذ"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_result_reaches_snackbar_without_resetting_editor(bool success)
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.Pickup, false);
        var editor = await Editor(await fixture.Read(id));
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<UpdateOrderCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? Result.Success() : Result.Failure("Rejected"));
        var snackbar = new Mock<ISnackbar>();
        SetService(editor, "RequestExecutor", executor.Object);
        SetService(editor, "Snackbar", snackbar.Object);
        var state = State(editor);
        await (Task)Method("SaveAsync").Invoke(editor, null);
        snackbar.Verify(x => x.Add(success ? "تم حفظ التعديلات بنجاح." : "Rejected",
            success ? Severity.Success : Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        Assert.Same(state, State(editor));
    }

    [Fact]
    public async Task Missing_order_returns_failure()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.Pickup, false);
        Assert.False((await fixture.UpdateOrder(id + 100, Build(await Editor(await fixture.Read(id))))).IsSuccess);
    }

    [Fact]
    public async Task Existing_increasing_step_numbers_are_preserved_and_stale_numbers_follow_list_order()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.DeliveryChain, false);
        var editor = await Editor(await fixture.Read(id));
        var steps = State(editor).Delivery.Steps;
        steps[0].StepOrder = 10;
        steps[1].StepOrder = 20;
        var input = Build(editor);
        Assert.Equal(new[] { 10, 20 }, input.DeliverySteps.Select(s => s.StepOrder));
        Assert.True((await fixture.UpdateOrder(id, input)).IsSuccess);
        Assert.Equal(new[] { 10, 20 }, (await fixture.Read(id)).DeliverySteps.Select(s => s.StepOrder));
        steps.Reverse();
        input = Build(editor);
        Assert.Equal(new[] { 1, 2 }, input.DeliverySteps.Select(s => s.StepOrder));
        Assert.Equal(new[] { 52, 51 }, input.DeliverySteps.Select(s => s.HandlerId));
    }

    private static async Task<OrderEditor> Editor(OrderEditorData dto)
    {
        var editor = new OrderEditor { OrderId = dto.Id };
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<GetOrderForEditQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        SetService(editor, "RequestExecutor", executor.Object);
        var state = await (Task<OrderEditorState>)Method("LoadOrderAsync").Invoke(editor, new object[] { dto.Id });
        typeof(OrderEditor).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(editor, state);
        return editor;
    }

    private static OrderEditorState State(OrderEditor editor) => (OrderEditorState)typeof(OrderEditor).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(editor);
    private static OrderInput.Order Build(OrderEditor editor)
    {
        Assert.Empty(Errors(State(editor)));
        return (OrderInput.Order)Method("BuildOrderInput").Invoke(editor, null);
    }
    private static string[] Errors(OrderEditorState state) => ((System.Collections.Generic.IReadOnlyList<string>)typeof(OrderEditor)
        .GetMethod("GetUpdateValidationErrors", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { state })).ToArray();
    private static MethodInfo Method(string name) => typeof(OrderEditor).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
    private static void SetService(OrderEditor editor, string name, object value) => typeof(OrderEditor).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(editor, value);
    private static void Set(object target, string name, object value) => target.GetType().GetProperty(name).SetValue(target, value);

    private sealed class Fixture : AppDbContextFactory, IDisposable
    {
        private readonly SqliteConnection _connection = new("Filename=:memory:;Foreign Keys=False");
        public Fixture() : base("")
        {
            _connection.Open();
            using var db = CreateDbContext();
            db.Database.EnsureCreated();
        }
        public override AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection).UseSnakeCaseNamingConvention().Options);
        public Task<OrderEditorData> Read(int id) => new OrderReader(this).GetForEditAsync(id, default);
        public async Task<Result> UpdateOrder(int id, OrderInput.Order input)
        {
            using var db = CreateDbContext();
            return await new UpdateOrderHandler(new OrderRepository(db), new UnitOfWork(db)).Handle(new(id, input), default);
        }
        public async Task<int> Seed(DeliveryMethod method, bool supplier)
        {
            using var db = CreateDbContext();
            var definition = Property.Create("Field", PropertyType.Text);
            var product = new Product("Current product", "P7", new Category("Category"), new Money(999));
            var supplierEntity = new Supplier("Renamed current supplier");
            if (supplier)
                product.AddSupplier(supplierEntity);
            db.Properties.Add(definition);
            db.Products.Add(product);
            await db.SaveChangesAsync();
            product.AddProperty(definition.Id, true);
            product.Properties.Single().Property = definition;
            var order = new Order(5, "ORD-UPDATE");
            order.UpdateHeader(5, OrderSource.Online, method,
                method == DeliveryMethod.DeliveryMan ? 31 : null,
                method == DeliveryMethod.ShippingCompany ? 32 : null, 41);
            var item = order.AddOrderItem(product.Id, "Saved product", 12.34m, 3, 14.75m,
                supplier ? "Historical supplier snapshot" : null, supplier ? supplierEntity.Id : null,
                [new(definition.Id, "Saved")]).Value;
            Set(item, nameof(OrderItem.Product), product);
            item.Properties.Single().Property = definition;
            if (method == DeliveryMethod.DeliveryChain)
            {
                order.AddDeliveryStep(new() { StepOrder = 2, DeliveryMethod = DeliveryMethod.ShippingCompany, ShippingCarrierId = 52 });
                order.AddDeliveryStep(new() { StepOrder = 1, DeliveryMethod = DeliveryMethod.DeliveryMan, DeliverymanId = 51 });
            }
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            return order.Id;
        }
        public void Dispose() => _connection.Dispose();
    }
}
