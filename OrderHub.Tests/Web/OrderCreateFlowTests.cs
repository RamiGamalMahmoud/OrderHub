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
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.Create;

using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
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

public class OrderCreateFlowTests
{
    [Theory]
    [InlineData(DeliveryMethod.Pickup, false)]
    [InlineData(DeliveryMethod.DeliveryMan, true)]
    [InlineData(DeliveryMethod.ShippingCompany, true)]
    [InlineData(DeliveryMethod.DeliveryChain, true)]
    public async Task Direct_create_preserves_new_editor_data_through_database_and_editor_reload(DeliveryMethod method, bool supplier)
    {
        using var fixture = new Fixture();
        var state = await fixture.State(method, supplier);
        var input = Build(Editor(state));
        var result = await fixture.Create(input);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.True(result.Value > 0);
        var dto = await new OrderReader(fixture).GetForEditAsync(result.Value, default);
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<GetOrderForEditQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        var editor = new OrderEditor();
        Service(editor, "RequestExecutor", executor.Object);
        var restored = await (Task<OrderEditorState>)Method("LoadOrderAsync").Invoke(editor, new object[] { result.Value });
        Assert.Equal(state.ClientId, restored.ClientId);
        Assert.Equal(state.OrderSource, restored.OrderSource);
        Assert.Equal(state.PaymentMethodId, restored.PaymentMethodId);
        Assert.Equal(method, restored.Delivery.Method);
        Assert.Equal(method == DeliveryMethod.DeliveryMan ? state.Delivery.DeliverymanId : null, restored.Delivery.DeliverymanId);
        Assert.Equal(method == DeliveryMethod.ShippingCompany ? state.Delivery.ShippingCarrierId : null, restored.Delivery.ShippingCarrierId);
        var saved = Assert.Single(restored.Items);
        var original = state.Items.Single();
        Assert.Equal(original.ProductId, saved.ProductId);
        Assert.Equal(original.ProductName, saved.ProductName);
        Assert.Equal(original.UnitPrice, saved.UnitPrice);
        Assert.Equal(original.Quantity, saved.Quantity);
        Assert.Equal(original.VatRate, saved.VatRate);
        Assert.Equal(original.SupplierId, saved.SupplierId);
        Assert.Equal(supplier ? "Current supplier" : null, dto.OrderItems.Single().SupplierName);
        Assert.Equal(original.Properties.Select(p => (p.PropertyId, p.Value)), saved.Properties.Select(p => (p.PropertyId, p.Value)));
        if (method == DeliveryMethod.DeliveryChain)
        {
            Assert.Equal(new[] { 1, 2 }, restored.Delivery.Steps.Select(s => s.StepOrder));
            Assert.Equal(new int?[] { 52, 51 }, restored.Delivery.Steps.Select(s => s.HandlerId));
            Assert.Equal(new[] { DeliveryMethod.ShippingCompany, DeliveryMethod.DeliveryMan }, restored.Delivery.Steps.Select(s => s.Method));
        }
        else
            Assert.Empty(restored.Delivery.Steps);
        Assert.Equal(31, state.Delivery.DeliverymanId);
        Assert.Equal(32, state.Delivery.ShippingCarrierId);
    }

    [Theory]
    [InlineData(DeliveryMethod.Pickup)]
    [InlineData(DeliveryMethod.DeliveryMan)]
    [InlineData(DeliveryMethod.ShippingCompany)]
    [InlineData(DeliveryMethod.DeliveryChain)]
    public async Task Handler_normalizes_delivery_even_when_a_caller_supplies_stale_data(DeliveryMethod method)
    {
        using var fixture = new Fixture();
        var input = Build(Editor(await fixture.State(method, false)));
        input = input with
        {
            DeliveryManId = 31,
            ShippingCarrierId = 32,
            DeliverySteps = [new(2, DeliveryMethod.ShippingCompany, 52), new(1, DeliveryMethod.DeliveryMan, 51)]
        };
        var result = await fixture.Create(input);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        using var db = fixture.CreateDbContext();
        var order = await db.Orders.Include(o => o.DeliverySteps).SingleAsync();
        Assert.Equal(method == DeliveryMethod.DeliveryMan ? (int?)31 : null, order.DeliverymanId);
        Assert.Equal(method == DeliveryMethod.ShippingCompany ? (int?)32 : null, order.ShippingCarrierId);
        Assert.Equal(method == DeliveryMethod.DeliveryChain ? 2 : 0, order.DeliverySteps.Count);
        if (method == DeliveryMethod.DeliveryChain)
        {
            var first = order.DeliverySteps.Single(s => s.StepOrder == 1);
            Assert.Equal(51, first.DeliverymanId);
            Assert.Null(first.ShippingCarrierId);
            var second = order.DeliverySteps.Single(s => s.StepOrder == 2);
            Assert.Equal(52, second.ShippingCarrierId);
            Assert.Null(second.DeliverymanId);
        }
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, 0)]
    public async Task Domain_failure_preserves_error_and_persists_no_partial_order(decimal price, int quantity)
    {
        using var fixture = new Fixture();
        var input = Build(Editor(await fixture.State(DeliveryMethod.DeliveryChain, false)));
        var valid = input.OrderItems.Single();
        input = input with { OrderItems = [valid, valid with { UnitPrice = price, Quantity = quantity }] };
        var result = await fixture.Create(input);
        Assert.False(result.IsSuccess);
        Assert.Equal(price == 0 ? "Unit price must be greater than zero." : "Quantity must be greater than zero.", result.ErrorMessage);
        await fixture.AssertNoOrder();
    }

    [Fact]
    public async Task Database_failure_returns_failure_and_rolls_back_the_aggregate()
    {
        using var fixture = new Fixture();
        var input = Build(Editor(await fixture.State(DeliveryMethod.DeliveryChain, false)));
        input = input with { OrderItems = [input.OrderItems.Single() with { ProductName = null }] };
        Assert.False((await fixture.Create(input)).IsSuccess);
        await fixture.AssertNoOrder();
    }

    [Fact]
    public async Task Materialized_input_survives_later_editor_changes_and_new_items_use_current_supplier_metadata()
    {
        using var fixture = new Fixture();
        var state = await fixture.State(DeliveryMethod.Pickup, true);
        var original = state.Items.Single();
        state.Items[0] = new OrderItemEditorState
        {
            ProductId = original.ProductId,
            ProductName = original.ProductName,
            CategoryName = original.CategoryName,
            UnitPrice = original.UnitPrice,
            Quantity = original.Quantity,
            VatRate = original.VatRate,
            SupplierId = original.SupplierId,
            PersistedSupplierId = original.SupplierId,
            SupplierName = "Historical snapshot",
            Suppliers = original.Suppliers,
            Properties = original.Properties
        };
        var editor = Editor(state);
        Assert.Null(editor.DraftId);
        var input = Build(editor);
        Assert.Equal("Current supplier", input.OrderItems.Single().SupplierName);
        state.Items.Single().Properties.Single().Value = "Changed";
        state.Items.Clear();
        Assert.Equal("Saved", input.OrderItems.Single().Properties.Single().Value);
        Assert.Empty(input.DeliverySteps);
    }

    [Fact]
    public async Task Unavailable_supplier_and_active_null_handler_block_final_create_but_empty_chain_remains_allowed()
    {
        using var fixture = new Fixture();
        var state = await fixture.State(DeliveryMethod.Pickup, false);
        state.Items.Single().SupplierId = 9999;
        Assert.NotEmpty(Errors(Editor(state)));
        state.Items.Single().SupplierId = null;
        state.Delivery.Steps.Single(s => s.Method == DeliveryMethod.DeliveryMan).HandlerId = null;
        Assert.Empty(Errors(Editor(state))); // Inactive steps do not participate in final mapping.
        state.Delivery.Method = DeliveryMethod.DeliveryChain;
        Assert.NotEmpty(Errors(Editor(state)));
        state.Delivery.Steps.Clear();
        var result = await fixture.Create(Build(Editor(state)));
        Assert.True(result.IsSuccess, result.ErrorMessage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Create_result_reaches_editor_with_null_draft_reference_and_preserves_state(bool success)
    {
        using var fixture = new Fixture();
        var state = await fixture.State(DeliveryMethod.Pickup, false);
        var editor = Editor(state);
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.Is<CreateOrderCommand>(c => c.OrderDraftId == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(success ? Result<int>.Success(42) : Result<int>.Failure("Rejected"));
        var snackbar = new Mock<ISnackbar>();
        Service(editor, "RequestExecutor", executor.Object);
        Service(editor, "Snackbar", snackbar.Object);
        await (Task)Method("SaveAsync").Invoke(editor, null);
        snackbar.Verify(x => x.Add(success ? "تم إنشاء الطلب بنجاح." : "Rejected",
            success ? Severity.Success : Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        Assert.Same(state, typeof(OrderEditor).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(editor));
    }

    private static OrderEditor Editor(OrderEditorState state)
    {
        var editor = new OrderEditor();
        typeof(OrderEditor).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(editor, state);
        return editor;
    }
    private static string[] Errors(OrderEditor editor) => ((System.Collections.Generic.IReadOnlyList<string>)typeof(OrderEditor)
        .GetProperty("ValidationErrors", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(editor)).ToArray();
    private static OrderInput.Order Build(OrderEditor editor)
    {
        Assert.Empty(Errors(editor));
        return (OrderInput.Order)Method("BuildOrderInput").Invoke(editor, null);
    }
    private static MethodInfo Method(string name) => typeof(OrderEditor).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
    private static void Service(OrderEditor editor, string name, object value) => typeof(OrderEditor).GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(editor, value);

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
        public async Task<Result<int>> Create(OrderInput.Order input)
        {
            using var db = CreateDbContext();
            // Strict mocks verify direct Create neither requires a user/draft nor calls linking services.
            var handler = new CreateOrderCommandHandler(new OrderRepository(db), new UnitOfWork(db),
                new Mock<IQuotationRepository>(MockBehavior.Strict).Object,
                new Mock<IProformaInvoiceRepository>(MockBehavior.Strict).Object,
                new Mock<ICurrentUser>(MockBehavior.Strict).Object,
                new Mock<IDraftRepository>(MockBehavior.Strict).Object);
            return await handler.Handle(new(null, input), default);
        }
        public async Task<OrderEditorState> State(DeliveryMethod method, bool withSupplier)
        {
            using var db = CreateDbContext();
            var definition = Property.Create("Field", PropertyType.Text);
            var product = new Product("Current product", "P7", new Category("Category"), new Money(999));
            var supplier = new Supplier("Current supplier");
            product.AddSupplier(supplier);
            db.Products.Add(product);
            db.Properties.Add(definition);
            await db.SaveChangesAsync();
            product.AddProperty(definition.Id, true);
            product.Properties.Single().Property = definition;
            await db.SaveChangesAsync();
            return new OrderEditorState
            {
                ClientId = 5,
                OrderSource = OrderSource.Online,
                PaymentMethodId = 41,
                Delivery = new()
                {
                    Method = method,
                    DeliverymanId = 31,
                    ShippingCarrierId = 32,
                    Steps = [new() { StepOrder = 8, Method = DeliveryMethod.ShippingCompany, HandlerId = 52 },
                        new() { StepOrder = 8, Method = DeliveryMethod.DeliveryMan, HandlerId = 51 }]
                },
                Items = [new() { ProductId = product.Id, ProductName = "Saved product", CategoryName = "Category",
                    UnitPrice = 12.34m, Quantity = 3, VatRate = 14.75m, SupplierId = withSupplier ? supplier.Id : null,
                    Suppliers = [new(supplier.Id, supplier.Name.Value)],
                    Properties = [new() { PropertyId = definition.Id, Name = "Field", IsRequired = true,
                        Type = PropertyType.Text, Value = "Saved" }] }]
            };
        }
        public async Task AssertNoOrder()
        {
            using var db = CreateDbContext();
            Assert.Empty(await db.Orders.ToListAsync());
            Assert.Empty(await db.OrderItems.ToListAsync());
            Assert.Empty(await db.OrderItemProperties.ToListAsync());
            Assert.Empty(await db.OrderDeliverySteps.ToListAsync());
        }
        public void Dispose() => _connection.Dispose();
    }
}
