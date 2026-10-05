using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Features.Orders.Queries;
using OrderHub.Application.Features.OrderDrafts.Get;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Web.Components.Pages.Features.Orders.Editor;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.Products;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Tests.Web;

public class OrderEditorSessionTests
{
    [Fact]
    public async Task Repeated_create_parameters_preserve_state_and_session()
    {
        var editor = new OrderEditor();
        await Parameters(editor);
        var state = (OrderEditorState)Field(editor, "_state");
        state.ClientId = 7;
        var session = Field(editor, "_sessionId");
        await Parameters(editor);
        Assert.Same(state, Field(editor, "_state"));
        Assert.Equal(session, Field(editor, "_sessionId"));
        Assert.Equal(7, state.ClientId);
    }

    [Fact]
    public async Task Different_drafts_orders_and_return_to_create_start_distinct_sessions()
    {
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<GetDraftQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetDraftQuery query, CancellationToken _) => new Draft(
                query.DraftId, 11, DateTime.UtcNow,
                new OrderDraftData(null, "", null, null, null, null, null, [], [])));
        executor.Setup(x => x.ExecuteAsync(It.IsAny<GetOrderEdit.Query>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetOrderEdit.Query query, CancellationToken _) =>
                new GetOrderEdit.Order(query.OrderId, 7, null, null, null, null, [], []));
        var editor = new OrderEditor();
        typeof(OrderEditor).GetProperty("RequestExecutor", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(editor, executor.Object);
        await Parameters(editor);
        var previous = Field(editor, "_state");
        editor.DraftId = Guid.NewGuid();
        await Parameters(editor);
        Assert.NotSame(previous, Field(editor, "_state"));
        previous = Field(editor, "_state");
        editor.DraftId = Guid.NewGuid();
        await Parameters(editor);
        Assert.NotSame(previous, Field(editor, "_state"));
        editor.DraftId = null;
        editor.OrderId = 1;
        await Parameters(editor);
        previous = Field(editor, "_state");
        editor.OrderId = 2;
        await Parameters(editor);
        Assert.NotSame(previous, Field(editor, "_state"));
        previous = Field(editor, "_state");
        editor.OrderId = null;
        await Parameters(editor);
        Assert.NotSame(previous, Field(editor, "_state"));
    }

    [Fact]
    public async Task Pending_add_cannot_commit_after_step_disposal()
    {
        var pending = new TaskCompletionSource<GetOrderItemEditor.OrderItemEditorDto>();
        var executor = new Mock<IRequestExecutor>();
        executor.Setup(x => x.ExecuteAsync(It.IsAny<GetOrderItemEditor.Query>(), It.IsAny<CancellationToken>())).Returns(pending.Task);
        using var step = new ProductsStep { Items = [], SessionId = Guid.NewGuid() };
        typeof(ProductsStep).GetProperty("RequestExecutor", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(step, executor.Object);
        typeof(ProductsStep).GetProperty("Logger", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(step, NullLogger<ProductsStep>.Instance);
        var add = (Task<Result>)typeof(ProductsStep).GetMethod("OnProductAddedAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(step, new object[] { new AddProductDialog.AddProductResult(new ProductLookupItem(7, "P", 5, "P7", "C", []), 5, 1, 0) });
        step.Dispose();
        pending.SetResult(new GetOrderItemEditor.OrderItemEditorDto(7, 5, "P", "C", [], []));
        Assert.False((await add).IsSuccess);
        Assert.Empty(step.Items);
    }

    private static Task Parameters(OrderEditor editor) => (Task)typeof(OrderEditor)
        .GetMethod("OnParametersSetAsync", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(editor, null);
    private static object Field(OrderEditor editor, string name) => typeof(OrderEditor)
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(editor);
}
