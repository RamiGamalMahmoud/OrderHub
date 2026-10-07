using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using MudBlazor;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Web.Components.Pages.Features.Orders.Workflow;

namespace OrderHub.Tests.Web;

public class OrderWorkflowDialogTests
{
    private static void Field(object component, string name, object value) => component.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(component, value);
    private static object Field(object component, string name) => component.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(component);
    private static Task Invoke(object component, string method) => (Task)component.GetType()
        .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
    private static void AttachDialog(object component, IMudDialogInstance dialog) => component.GetType()
        .GetProperty("Dialog", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(component, dialog);

    [Fact]
    public async Task Reason_dialog_rejects_empty_and_keeps_reason_after_persistence_failure()
    {
        var instance = new Mock<IMudDialogInstance>();
        var calls = 0;
        var dialog = new OrderReasonDialog { Submit = _ => { calls++; return Task.FromResult(Result<OrderOperationReceipt>.Failure("Rejected", "Persistence")); } };
        AttachDialog(dialog, instance.Object);
        Field(dialog, "_reason", "   ");
        await Invoke(dialog, "ConfirmAsync");
        Assert.Equal(0, calls);
        Field(dialog, "_reason", "User reason");
        await Invoke(dialog, "ConfirmAsync");
        Assert.Equal(1, calls);
        Assert.Equal("User reason", Field(dialog, "_reason"));
        Assert.Equal("Rejected", Field(dialog, "_error"));
        instance.Verify(value => value.Close(It.IsAny<DialogResult>()), Times.Never);
        dialog.Submit = _ => Task.FromResult(Result<OrderOperationReceipt>.Failure("Stale", "Conflict"));
        await Invoke(dialog, "ConfirmAsync");
        instance.Verify(value => value.Cancel(), Times.Once);
    }

    [Fact]
    public async Task Direct_delivery_defaults_preserve_states_and_bulk_choice_does_not_submit()
    {
        var state = new OrderWorkflowState(1, "TEST", DateTime.UtcNow, OrderStatus.OutForDelivery,
            DeliveryMethod.DeliveryChain, null, 2, false, false, false, null, [],
            [new(10, 1, DeliveryMethod.DeliveryMan, "First", DeliveryStatus.Completed, [], null, null, null, null, false),
             new(20, 2, DeliveryMethod.DeliveryMan, "Second", DeliveryStatus.Pending, [], null, null, null, null, false)],
            DateTime.UtcNow, null, false, null, null, null, true);
        var calls = 0;
        IReadOnlyList<DeliveryStepOutcome> captured = null;
        var instance = new Mock<IMudDialogInstance>();
        var dialog = new DirectDeliveryDialog
        {
            State = state,
            Submit = (outcomes, reason) => { calls++; captured = outcomes; return Task.FromResult(Result<OrderOperationReceipt>.Failure("Rejected", "Persistence")); }
        };
        AttachDialog(dialog, instance.Object);
        typeof(DirectDeliveryDialog).GetMethod("OnParametersSet", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(dialog, null);
        var outcomes = ((IEnumerable)Field(dialog, "_outcomes")).Cast<object>().ToList();
        Assert.Single(outcomes);
        Field(dialog, "_reason", "Confirmed actual delivery");
        await Invoke(dialog, "ConfirmAsync");
        Assert.Equal(20, captured.Single().StepId);
        Assert.Equal(DeliveryStatus.Pending, captured.Single().Status);
        Assert.Equal("Confirmed actual delivery", Field(dialog, "_reason"));
        typeof(DirectDeliveryDialog).GetMethod("CompleteRemaining", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(dialog, null);
        Assert.Equal(1, calls);
        await Invoke(dialog, "ConfirmAsync");
        Assert.Equal(DeliveryStatus.Completed, captured.Single().Status);
        Assert.Equal(2, calls);
        var editor = outcomes.Single();
        editor.GetType().GetProperty("Status").SetValue(editor, DeliveryStatus.Failed);
        await Invoke(dialog, "ConfirmAsync");
        Assert.Equal(2, calls); // The general reason is not copied silently.
        editor.GetType().GetProperty("UseCommonReason").SetValue(editor, true);
        await Invoke(dialog, "ConfirmAsync");
        Assert.Equal(3, calls);
        Assert.Equal("Confirmed actual delivery", captured.Single().Reason);
        instance.Verify(value => value.Close(It.IsAny<DialogResult>()), Times.Never);
    }

    [Fact]
    public async Task Pending_submission_does_not_send_a_second_request()
    {
        var pending = new TaskCompletionSource<Result<OrderOperationReceipt>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var instance = new Mock<IMudDialogInstance>();
        var dialog = new OrderReasonDialog { Submit = _ => { calls++; return pending.Task; } };
        AttachDialog(dialog, instance.Object);
        Field(dialog, "_reason", "Reason");
        var first = Invoke(dialog, "ConfirmAsync");
        await Invoke(dialog, "ConfirmAsync");
        Assert.Equal(1, calls);
        pending.SetResult(Result<OrderOperationReceipt>.Success(new(1, 2, OrderStatus.Cancelled)));
        await first;
        instance.Verify(value => value.Close(It.IsAny<DialogResult>()), Times.Once);
    }
}
