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
using OrderHub.Application.Features.OrderDrafts.Save;
using OrderHub.Application.Features.OrderDrafts.Get;
using OrderHub.Application.Features.OrderDrafts.List;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.Create;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.OrderDrafts;
using OrderHub.Infrastructure.Services;
using OrderHub.Infrastructure.Stores;
using OrderHub.Web.Components.Pages.Features.Orders.Editor;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Tests.Web;

public class DraftCompletionLifecycleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task New_and_continued_sessions_save_and_consume_one_identity(bool continued)
    {
        using var fixture = new Fixture();
        var editor = fixture.Editor();
        await Invoke(editor, "OnParametersSetAsync");
        State(editor, fixture.State());
        await Invoke(editor, "SaveDraft");
        var draftId = (Guid)Field(editor, "_sessionId");
        Assert.NotNull(await fixture.Store.GetAsync(draftId, 11));
        if (continued)
        {
            editor.Dispose();
            editor = fixture.Editor();
            editor.DraftId = draftId;
            await Invoke(editor, "OnParametersSetAsync");
        }
        GetState(editor).Items.Single().Quantity = 2;
        await Invoke(editor, "OnEditorChanged");
        using (var db = fixture.CreateDbContext())
            Assert.Equal(draftId, (await db.DraftRecords.SingleAsync()).Id);
        await Invoke(editor, "SaveAsync");
        Assert.Equal(draftId, fixture.LastCreate.OrderDraftId);
        Assert.Null(await fixture.Store.GetAsync(draftId, 11));
        await Invoke(editor, "OnEditorChanged");
        await Invoke(editor, "SaveDraft");
        await Invoke(editor, "SaveAsync");
        using var check = fixture.CreateDbContext();
        Assert.Single(await check.Orders.ToListAsync());
        Assert.Empty(await check.DraftRecords.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Domain_and_database_failure_leave_draft_and_documents_recoverable(bool databaseFailure)
    {
        using var fixture = new Fixture();
        var editor = fixture.Editor();
        await Invoke(editor, "OnParametersSetAsync");
        State(editor, fixture.State());
        await Invoke(editor, "SaveDraft");
        var id = (Guid)Field(editor, "_sessionId");
        await fixture.Documents(id);
        var input = (OrderInput.Order)Method("BuildOrderInput").Invoke(editor, null);
        input = input with { OrderItems = [input.OrderItems.Single() with
            { ProductName = databaseFailure ? null : "Product", UnitPrice = databaseFailure ? 10 : 0 }] };
        Assert.False((await fixture.Create(new(id, input))).IsSuccess);
        Assert.NotNull(await fixture.Store.GetAsync(id, 11));
        using var check = fixture.CreateDbContext();
        Assert.Empty(await check.Orders.ToListAsync());
        Assert.Empty(await check.OrderItems.ToListAsync());
        Assert.Equal(id, (await check.Quotations.SingleAsync()).SourceDraftReference);
        Assert.Equal(id, (await check.ProformaInvoices.SingleAsync()).SourceDraftReference);
    }

    [Fact]
    public async Task Successful_completion_links_both_documents_and_consumes_draft_in_one_commit()
    {
        using var fixture = new Fixture();
        var editor = fixture.Editor();
        await Invoke(editor, "OnParametersSetAsync");
        State(editor, fixture.State());
        await Invoke(editor, "SaveDraft");
        var id = (Guid)Field(editor, "_sessionId");
        await fixture.Documents(id);
        await Invoke(editor, "SaveAsync");
        using var check = fixture.CreateDbContext();
        var order = await check.Orders.SingleAsync();
        var quotation = await check.Quotations.SingleAsync();
        var proforma = await check.ProformaInvoices.SingleAsync();
        Assert.Equal(order.Id, quotation.OrderId);
        Assert.Equal(order.Id, proforma.OrderId);
        Assert.Null(quotation.SourceDraftReference);
        Assert.Null(proforma.SourceDraftReference);
        Assert.Empty(await check.DraftRecords.ToListAsync());
    }

    [Fact]
    public async Task Linking_failure_never_consumes_the_draft()
    {
        using var fixture = new Fixture();
        var editor = fixture.Editor();
        await Invoke(editor, "OnParametersSetAsync");
        State(editor, fixture.State());
        await Invoke(editor, "SaveDraft");
        var id = (Guid)Field(editor, "_sessionId");
        var input = (OrderInput.Order)Method("BuildOrderInput").Invoke(editor, null);
        var failing = new Mock<IProformaInvoiceRepository>();
        failing.Setup(r => r.GetByDraftReference(id, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Link read failed"));
        Assert.False((await fixture.Create(new(id, input), failing.Object)).IsSuccess);
        Assert.NotNull(await fixture.Store.GetAsync(id, 11));
    }

    [Fact]
    public async Task Completion_drains_an_inflight_autosave_before_consumption_and_blocks_later_callbacks()
    {
        using var fixture = new Fixture();
        var editor = fixture.Editor();
        await Invoke(editor, "OnParametersSetAsync");
        State(editor, fixture.State());
        var started = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(s => s.ExecuteAsync(It.IsAny<SaveDraftCommand>(), It.IsAny<CancellationToken>())).Returns(async (SaveDraftCommand command, CancellationToken _) =>
        {
            started.SetResult();
            await release.Task;
            await fixture.Store.SaveAsync(new Draft(command.DraftId, 11, DateTime.UtcNow, command.Data));
            return Result.Success();
        });
        executor.Setup(e => e.ExecuteAsync(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
            .Returns((CreateOrderCommand command, CancellationToken _) => fixture.Create(command));
        Service(editor, "RequestExecutor", executor.Object);
        var autosave = Invoke(editor, "SaveDraft");
        await started.Task;
        var create = Invoke(editor, "SaveAsync");
        Assert.Null(fixture.LastCreate);
        await Invoke(editor, "OnEditorChanged");
        release.SetResult();
        await Task.WhenAll(autosave, create);
        var id = (Guid)Field(editor, "_sessionId");
        Assert.Equal(id, fixture.LastCreate.OrderDraftId);
        Assert.Null(await fixture.Store.GetAsync(id, 11));
        await Invoke(editor, "SaveDraft");
        executor.Verify(s => s.ExecuteAsync(It.IsAny<SaveDraftCommand>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Failed_create_keeps_identity_active_and_allows_retry()
    {
        using var fixture = new Fixture();
        var editor = fixture.Editor();
        await Invoke(editor, "OnParametersSetAsync");
        State(editor, fixture.State());
        await Invoke(editor, "SaveDraft");
        var id = (Guid)Field(editor, "_sessionId");
        var failing = new Mock<IRequestExecutor>();
        failing.Setup(e => e.ExecuteAsync(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result<int>.Failure("Rejected"));
        failing.Setup(e => e.ExecuteAsync(It.IsAny<SaveDraftCommand>(), It.IsAny<CancellationToken>()))
            .Returns((SaveDraftCommand command, CancellationToken _) => fixture.Save(command));
        Service(editor, "RequestExecutor", failing.Object);
        var state = GetState(editor);
        await Invoke(editor, "SaveAsync");
        Assert.Same(state, GetState(editor));
        Assert.NotNull(await fixture.Store.GetAsync(id, 11));
        state.Items.Single().Quantity = 3;
        await Invoke(editor, "SaveDraft");
        Assert.Equal(3, (await fixture.Store.GetAsync(id, 11)).Data.Items.Single().Quantity);
        Service(editor, "RequestExecutor", fixture.Executor.Object);
        await Invoke(editor, "SaveAsync");
        Assert.Equal(id, fixture.LastCreate.OrderDraftId);
        Assert.Null(await fixture.Store.GetAsync(id, 11));
    }

    [Fact]
    public async Task Direct_create_does_not_invent_a_persisted_draft_and_new_route_resets_completion()
    {
        using var fixture = new Fixture();
        var editor = fixture.Editor();
        await Invoke(editor, "OnParametersSetAsync");
        State(editor, fixture.State());
        await Invoke(editor, "SaveAsync");
        Assert.Null(fixture.LastCreate.OrderDraftId);
        // Move through a different route identity and back to New on the same instance.
        var existing = fixture.State();
        var draftId = Guid.NewGuid();
        await fixture.Store.SaveAsync(new Draft(draftId, 11, DateTime.UtcNow,
            new(null, "", null, null, null, null, null, [], [])));
        editor.DraftId = draftId;
        await Invoke(editor, "OnParametersSetAsync");
        editor.DraftId = null;
        await Invoke(editor, "OnParametersSetAsync");
        State(editor, existing);
        await Invoke(editor, "SaveDraft");
        Assert.NotNull(await fixture.Store.GetAsync((Guid)Field(editor, "_sessionId"), 11));
        editor.Dispose();
        var saves = await fixture.Store.GetUserDraftsAsync(11);
        await Invoke(editor, "SaveDraft");
        Assert.Equal(saves.Count, (await fixture.Store.GetUserDraftsAsync(11)).Count);
    }

    private static MethodInfo Method(string name) => typeof(OrderEditor).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static Task Invoke(OrderEditor editor, string name) => (Task)Method(name).Invoke(editor, null);
    private static object Field(OrderEditor editor, string name) => typeof(OrderEditor).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(editor);
    private static OrderEditorState GetState(OrderEditor editor) => (OrderEditorState)Field(editor, "_state");
    private static void State(OrderEditor editor, OrderEditorState state) => typeof(OrderEditor).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, state);
    private static void Service(OrderEditor editor, string name, object value) => typeof(OrderEditor).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(editor, value);

    private sealed class Fixture : AppDbContextFactory, IDisposable
    {
        private readonly SqliteConnection _connection = new("Filename=:memory:;Foreign Keys=False");
        private readonly Mock<ICurrentUser> _user = new();
        private readonly int _productId;
        public DraftAccess Store { get; }
        public Mock<IRequestExecutor> Executor { get; } = new();
        public CreateOrderCommand LastCreate { get; private set; }
        public Fixture() : base("")
        {
            _connection.Open();
            using var db = CreateDbContext();
            db.Database.EnsureCreated();
            var product = new Product("Product", "P1", new Category("Category"), new Money(10));
            db.Products.Add(product);
            db.SaveChanges();
            _productId = product.Id;
            Store = new DraftAccess(this);
            _user.SetupGet(u => u.Id).Returns(11);
            Executor.Setup(e => e.ExecuteAsync(It.IsAny<CreateOrderCommand>(), It.IsAny<CancellationToken>()))
                .Returns((CreateOrderCommand command, CancellationToken _) => Create(command));
            Executor.Setup(e => e.ExecuteAsync(It.IsAny<SaveDraftCommand>(), It.IsAny<CancellationToken>()))
                .Returns((SaveDraftCommand command, CancellationToken _) => Save(command));
            Executor.Setup(e => e.ExecuteAsync(It.IsAny<GetDraftQuery>(), It.IsAny<CancellationToken>()))
                .Returns((GetDraftQuery query, CancellationToken _) => Store.GetAsync(query.DraftId, 11));
            Executor.Setup(e => e.ExecuteAsync(It.IsAny<GetOrderItemEditor.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetOrderItemEditor.OrderItemEditorDto(_productId, 10, "Product", "Category", [], []));
        }
        public override AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).UseSnakeCaseNamingConvention().Options);
        public OrderEditorState State() => new()
        {
            ClientId = 5, OrderSource = OrderSource.Online, Delivery = new() { Method = DeliveryMethod.Pickup },
            Items = [new() { ProductId = _productId, ProductName = "Product", CategoryName = "Category", UnitPrice = 10, Quantity = 1 }]
        };
        public OrderEditor Editor()
        {
            var editor = new OrderEditor();
            Service(editor, "RequestExecutor", Executor.Object);
            Service(editor, "Snackbar", new Mock<ISnackbar>().Object);
            return editor;
        }
        public async Task<Result<int>> Create(CreateOrderCommand command, IProformaInvoiceRepository proforma = null)
        {
            LastCreate = command;
            using var db = CreateDbContext();
            var handler = new CreateOrderCommandHandler(new OrderRepository(db), new UnitOfWork(db),
                new QuotationRepository(db), proforma ?? new ProformaInvoiceRepository(db), _user.Object, new DraftRepository(db));
            return await handler.Handle(command, default);
        }
        public async Task<Result> Save(SaveDraftCommand command)
        {
            using var db = CreateDbContext();
            return await new SaveDraftCommandHandler(_user.Object, new DraftRepository(db), new UnitOfWork(db)).Handle(command, default);
        }
        public sealed class DraftAccess(Fixture fixture)
        {
            public async Task SaveAsync(Draft draft)
            {
                var result = await fixture.Save(new SaveDraftCommand(draft.Id, draft.Data));
                if (!result.IsSuccess) throw new InvalidOperationException(result.ErrorMessage);
            }
            public Task<Draft> GetAsync(Guid id, int userId) => new GetDraftQueryHandler(
                fixture._user.Object, new DraftReadService(fixture)).Handle(new(id), default);
            public Task<System.Collections.Generic.IReadOnlyList<OrderDraftSummary>> GetUserDraftsAsync(int userId) =>
                new GetDraftListQueryHandler(fixture._user.Object, new DraftReadService(fixture)).Handle(new(), default);
        }
        public async Task Documents(Guid draftId)
        {
            using var db = CreateDbContext();
            db.Quotations.Add(Quotation.Create("QT-1", DateTime.UtcNow, "Customer", "Phone", "Address", 10, 0, 10, DateTime.UtcNow.AddDays(1), draftId));
            db.ProformaInvoices.Add(ProformaInvoice.Create("PF-1", DateTime.UtcNow, "Customer", "Phone", "Address", 10, 0, 10, draftId));
            await db.SaveChangesAsync();
        }
        public void Dispose() => _connection.Dispose();
    }
}
