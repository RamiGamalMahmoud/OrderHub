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
                .Handle(new(id, input, order.Version), default);
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
            .ReturnsAsync(success ? Result<OrderUpdateReceipt>.Success(new(2, [])) : Result<OrderUpdateReceipt>.Failure("Rejected"));
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

    [Fact]
    public async Task Swapping_ranks_and_changing_handler_preserves_step_identity_and_creation_time()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.DeliveryChain, false);
        var before = await fixture.Read(id);
        using var check = fixture.CreateDbContext();
        var created = await check.OrderDeliverySteps.AsNoTracking().ToDictionaryAsync(step => step.Id, step => step.CreatedAt);
        var editor = await Editor(before);
        State(editor).Delivery.Steps.Reverse();
        State(editor).Delivery.Steps[0].Method = DeliveryMethod.DeliveryMan;
        State(editor).Delivery.Steps[0].HandlerId = 61;
        Assert.True((await fixture.UpdateOrder(id, Build(editor))).IsSuccess);
        var after = await fixture.Read(id);
        Assert.Equal(before.DeliverySteps.Reverse().Select(step => step.Id), after.DeliverySteps.Select(step => step.Id));
        Assert.Equal(new[] { 1, 2 }, after.DeliverySteps.Select(step => step.StepOrder));
        Assert.Equal(61, after.DeliverySteps.First().HandlerId);
        var rows = await check.OrderDeliverySteps.AsNoTracking().ToListAsync();
        Assert.All(rows, step => Assert.Equal(created[step.Id], step.CreatedAt));
        Assert.All(rows, step => Assert.Null(step.ShippingCarrierId));
    }

    [Fact]
    public async Task Add_and_remove_at_an_occupied_rank_changes_only_the_selected_steps()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.DeliveryChain, false);
        var before = await fixture.Read(id);
        var input = Build(await Editor(before));
        input = input with { DeliverySteps = [input.DeliverySteps.First() with { StepOrder = 2 }, new(1, DeliveryMethod.DeliveryMan, 63)] };
        Assert.True((await fixture.UpdateOrder(id, input)).IsSuccess);
        var after = await fixture.Read(id);
        Assert.Equal(before.DeliverySteps.First().Id, after.DeliverySteps.Last().Id);
        Assert.DoesNotContain(after.DeliverySteps, step => step.Id == before.DeliverySteps.Last().Id);
        Assert.DoesNotContain(before.DeliverySteps, step => step.Id == after.DeliverySteps.First().Id);
        Assert.True((await fixture.UpdateOrder(id, Build(await Editor(after)))).IsSuccess);
        Assert.Equal(after.DeliverySteps, (await fixture.Read(id)).DeliverySteps);
    }

    [Theory]
    [InlineData("foreign")]
    [InlineData("duplicate")]
    [InlineData("zero")]
    [InlineData("handler")]
    public async Task Invalid_step_identity_or_handler_leaves_the_tracked_order_unchanged(string invalid)
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.DeliveryChain, false);
        int foreignStepId;
        using (var otherDb = fixture.CreateDbContext())
        {
            var other = new Order(5, "OTHER-ORDER");
            var foreignStep = new OrderDeliveryStep { StepOrder = 1, DeliveryMethod = DeliveryMethod.DeliveryMan, DeliverymanId = 51 };
            other.AddDeliveryStep(foreignStep);
            otherDb.Orders.Add(other);
            await otherDb.SaveChangesAsync();
            foreignStepId = foreignStep.Id;
        }
        var before = await fixture.Read(id);
        var steps = Build(await Editor(before)).DeliverySteps.ToList();
        steps[1] = invalid switch
        {
            "foreign" => steps[1] with { Id = foreignStepId },
            "duplicate" => steps[1] with { Id = steps[0].Id },
            "zero" => steps[1] with { Id = 0 },
            _ => steps[1] with { HandlerId = 0 }
        };
        var input = Build(await Editor(before)) with { DeliverySteps = steps, OrderSource = OrderSource.Salla };
        using var db = fixture.CreateDbContext();
        var repository = new OrderRepository(db);
        var order = await repository.GetForUpdateAsync(id);
        var originalSteps = order.DeliverySteps.ToList();
        Assert.False((await new UpdateOrderHandler(repository, new UnitOfWork(db)).Handle(new(id, input, order.Version), default)).IsSuccess);
        Assert.Equal(originalSteps, order.DeliverySteps);
        Assert.All(db.ChangeTracker.Entries(), entry => Assert.Equal(EntityState.Unchanged, entry.State));
        await db.SaveChangesAsync();
        Assert.Equal(before.DeliverySteps, (await fixture.Read(id)).DeliverySteps);
        Assert.Equal(before.OrderSource, (await fixture.Read(id)).OrderSource);
    }

    [Fact]
    public async Task Failure_after_temporary_reordering_rolls_back_all_ranks_and_header()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.DeliveryChain, false);
        var before = await fixture.Read(id);
        var editor = await Editor(before);
        State(editor).Delivery.Steps.Reverse();
        var input = Build(editor);
        input = input with { OrderSource = OrderSource.Salla, OrderItems = [input.OrderItems.Single() with { ProductName = null }] };
        Assert.False((await fixture.UpdateOrder(id, input)).IsSuccess);
        var after = await fixture.Read(id);
        Assert.Equal(before.DeliverySteps, after.DeliverySteps);
        Assert.Equal(before.OrderSource, after.OrderSource);
    }

    [Fact]
    public async Task Saving_twice_from_the_same_editor_reuses_new_step_identity()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.DeliveryChain, false);
        var editor = await Editor(await fixture.Read(id));
        State(editor).Delivery.Steps.Add(new() { StepOrder = 3, Method = DeliveryMethod.DeliveryMan, HandlerId = 63 });
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<UpdateOrderCommand>(), It.IsAny<CancellationToken>()))
            .Returns((UpdateOrderCommand command, CancellationToken _) => fixture.UpdateOrder(command.OrderId, command.Order, command.ExpectedVersion));
        executor.Setup(x => x.ExecuteAsync(It.IsAny<GetOrderForEditQuery>(), It.IsAny<CancellationToken>()))
            .Returns(() => fixture.Read(id));
        SetService(editor, "RequestExecutor", executor.Object);
        SetService(editor, "Snackbar", new Mock<ISnackbar>().Object);
        await (Task)Method("SaveAsync").Invoke(editor, null);
        var first = await fixture.Read(id);
        Assert.All(State(editor).Delivery.Steps, step => Assert.True(step.Id > 0));
        await (Task)Method("SaveAsync").Invoke(editor, null);
        Assert.Equal(first.DeliverySteps, (await fixture.Read(id)).DeliverySteps);
    }

    [Fact]
    public async Task Repeated_save_click_during_update_sends_only_one_request_and_failure_allows_retry()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.DeliveryChain, false);
        var editor = await Editor(await fixture.Read(id));
        var pending = new TaskCompletionSource<Result<OrderUpdateReceipt>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<UpdateOrderCommand>(), It.IsAny<CancellationToken>())).Returns(pending.Task);
        SetService(editor, "RequestExecutor", executor.Object);
        SetService(editor, "Snackbar", new Mock<ISnackbar>().Object);
        var saving = (Task)Method("SaveAsync").Invoke(editor, null);
        await (Task)Method("SaveAsync").Invoke(editor, null);
        executor.Verify(x => x.ExecuteAsync(It.IsAny<UpdateOrderCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        pending.SetResult(Result<OrderUpdateReceipt>.Failure("Rejected"));
        await saving;
        await (Task)Method("SaveAsync").Invoke(editor, null);
        executor.Verify(x => x.ExecuteAsync(It.IsAny<UpdateOrderCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Stale_editor_cannot_replace_saved_steps_or_items()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.DeliveryChain, false);
        var original = await fixture.Read(id);
        var input = Build(await Editor(original));
        Assert.True((await fixture.UpdateOrder(id, input, original.Version)).IsSuccess);
        var after = await fixture.Read(id);
        Assert.False((await fixture.UpdateOrder(id, input with { OrderSource = OrderSource.Salla }, original.Version)).IsSuccess);
        var final = await fixture.Read(id);
        Assert.Equal(after.Version, final.Version);
        Assert.Equal(after.DeliverySteps, final.DeliverySteps);
        Assert.Equal(after.OrderSource, final.OrderSource);
    }

    [Fact]
    public async Task Active_order_rejects_editor_and_separate_payment_writes()
    {
        using var fixture = new Fixture();
        var id = await fixture.Seed(DeliveryMethod.DeliveryChain, false);
        var input = Build(await Editor(await fixture.Read(id)));
        using (var db = fixture.CreateDbContext())
        {
            var order = await new OrderRepository(db).GetForUpdateAsync(id);
            Assert.True(order.TransitionTo(OrderStatus.OutForDelivery, new(1, "Operator", DateTime.UtcNow)).IsSuccess);
            await db.SaveChangesAsync();
        }
        var before = await fixture.Read(id);
        Assert.False(State(await Editor(before)).CanEdit);
        Assert.False((await fixture.UpdateOrder(id, input, before.Version)).IsSuccess);
        using (var db = fixture.CreateDbContext())
        {
            var result = await new OrderHub.Application.Features.Orders.ChangePaymentMethod.ChangePaymentMethodCommandHandler(
                new OrderRepository(db), new UnitOfWork(db)).Handle(new(id, 99, before.Version), default);
            Assert.False(result.IsSuccess);
            await db.SaveChangesAsync();
        }
        var after = await fixture.Read(id);
        Assert.Equal(before.Version, after.Version);
        Assert.Equal(before.Status, after.Status);
        Assert.Equal(before.PaymentMothodId, after.PaymentMothodId);
        Assert.Equal(before.DeliverySteps, after.DeliverySteps);
        Assert.Equal(before.OrderItems.Select(item => (item.ProductId, item.ProductName, item.Quantity, item.UnitPrice)),
            after.OrderItems.Select(item => (item.ProductId, item.ProductName, item.Quantity, item.UnitPrice)));
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
        public async Task<Result<OrderUpdateReceipt>> UpdateOrder(int id, OrderInput.Order input, int? expectedVersion = null)
        {
            using var db = CreateDbContext();
            var order = await new OrderRepository(db).GetForUpdateAsync(id);
            return await new UpdateOrderHandler(new OrderRepository(db), new UnitOfWork(db)).Handle(new(id, input, expectedVersion ?? order?.Version ?? 1), default);
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
