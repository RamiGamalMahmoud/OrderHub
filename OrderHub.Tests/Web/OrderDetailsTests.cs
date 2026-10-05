using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Moq;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.OrderDetails;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Tests.Web;

public class OrderDetailsTests
{
    [Fact]
    public async Task Lookup_failure_preserves_input_and_can_be_retried()
    {
        var state = new OrderEditorState { ClientId = 7 };
        using var details = new DetailsStep { State = state };
        var lookups = new Moq.Mock<OrderHub.Application.Interfaces.ILookupService>();
        lookups.SetupSequence(x => x.GetClients())
            .ThrowsAsync(new InvalidOperationException("Unavailable"))
            .ReturnsAsync(Array.Empty<OrderHub.Application.Common.Lookups.ClientLookup>());
        lookups.Setup(x => x.GetPaymentMethods()).ReturnsAsync(Array.Empty<OrderHub.Application.Common.Lookups.PaymentMethodLookup>());
        lookups.Setup(x => x.GetDeliveryMethods()).ReturnsAsync(Array.Empty<OrderHub.Application.Common.Lookups.DeliverymanLookup>());
        lookups.Setup(x => x.GetShippingCarriers()).ReturnsAsync(Array.Empty<OrderHub.Application.Common.Lookups.ShippingCarrierLookup>());
        typeof(DetailsStep).GetProperty("Lookups", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(details, lookups.Object);
        typeof(DetailsStep).GetProperty("Logger", BindingFlags.NonPublic | BindingFlags.Instance)
            .SetValue(details, Microsoft.Extensions.Logging.Abstractions.NullLogger<DetailsStep>.Instance);
        await Invoke(details, "LoadLookupsAsync");
        Assert.NotNull(typeof(DetailsStep).GetField("_error", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(details));
        Assert.Equal(7, state.ClientId);
        await Invoke(details, "LoadLookupsAsync");
        Assert.Null(typeof(DetailsStep).GetField("_error", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(details));
        Assert.Equal(7, state.ClientId);
    }

    private static Task Invoke(object component, string name, params object[] arguments) =>
        (Task)component.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);

    [Fact]
    public async Task Details_survive_section_recreation_and_only_changed_values_notify()
    {
        var state = new OrderEditorState();
        var notifications = 0;
        var callback = EventCallback.Factory.Create(new object(), () => notifications++);
        using (var details = new DetailsStep { State = state, DetailsChanged = callback })
        {
            await Invoke(details, "SetClientAsync", (int?)7);
            await Invoke(details, "SetClientAsync", (int?)7);
            await Invoke(details, "SetPaymentAsync", (int?)8);
            await Invoke(details, "SetSourceAsync", (OrderSource?)OrderSource.Online);
            var delivery = new DeliverySection { Delivery = state.Delivery, Changed = callback };
            var chain = new DeliveryChainEditor { Steps = state.Delivery.Steps, Changed = callback };
            await Invoke(delivery, "OnDeliveryMethodChangedAsync", (DeliveryMethod?)DeliveryMethod.DeliveryChain);
            await Invoke(chain, "AddStepAsync");
            Assert.Null(state.Delivery.Steps[0].HandlerId);
            Assert.Equal(5, notifications);
        }
        using var reopened = new DetailsStep { State = state, DetailsChanged = callback };
        Assert.Equal(7, reopened.State.ClientId);
        Assert.Equal(8, reopened.State.PaymentMethodId);
        Assert.Equal(OrderSource.Online, reopened.State.OrderSource);
        Assert.Single(reopened.State.Delivery.Steps);
        await Invoke(reopened, "SetClientAsync", new object[] { null });
        Assert.Null(state.ClientId);
        Assert.Equal(6, notifications);
    }

    [Fact]
    public async Task Chain_moves_preserve_handlers_and_changing_method_clears_incompatible_handler()
    {
        var first = new DeliveryStepEditorState { Method = DeliveryMethod.DeliveryMan, HandlerId = 1 };
        var second = new DeliveryStepEditorState { Method = DeliveryMethod.ShippingCompany, HandlerId = 2 };
        var state = new OrderEditorState();
        state.Delivery.Steps.AddRange([first, second]);
        var notifications = 0;
        using var details = new DetailsStep
        {
            State = state, DetailsChanged = EventCallback.Factory.Create(new object(), () => notifications++)
        };
        var callback = details.DetailsChanged;
        var chain = new DeliveryChainEditor { Steps = state.Delivery.Steps, Changed = callback };
        var step = new DeliveryChainStep { Step = first, Changed = callback };
        await Invoke(chain, "MoveStepAsync", first, -1);
        Assert.Equal(0, notifications);
        await Invoke(chain, "MoveStepAsync", first, 1);
        Assert.Same(second, state.Delivery.Steps[0]);
        Assert.Equal(1, first.HandlerId);
        await Invoke(step, "OnMethodChangedAsync", DeliveryMethod.ShippingCompany);
        Assert.Null(first.HandlerId);
        await Invoke(step, "OnHandlerChangedAsync", (int?)3);
        await Invoke(chain, "RemoveStepAsync", first);
        await Invoke(chain, "MoveStepAsync", first, 1);
        await Invoke(chain, "RemoveStepAsync", first);
        Assert.Equal(3, first.HandlerId);
        Assert.Equal(4, notifications);
        details.Dispose();
        await Invoke(details, "SetClientAsync", (int?)9);
        Assert.Null(state.ClientId);
    }
}
