using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Features.Orders.Editor;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.OrderDtos;

namespace OrderHub.UI.Features.Orders.Edit;

internal class ViewModel : Editor.ViewModel
{
    private readonly ISelectionStore<IOrderMarker, int> _selectionStore;

    public ViewModel(
        IMediator mediator,
        IDialogService dialogService,
        IMessenger messenger,
        ISelectionStore<IOrderMarker, int> selectionStore) : base(mediator, dialogService, messenger)
    {
        _selectionStore = selectionStore;
    }

    public override string ActionName => "حفظ التعديلات";
    public override string Title => "تعديل طلب";

    protected override async Task AfterLoadAsync()
    {
        OrderEditDto order = await _mediator.Send(new Application.Queries.OrderQueries.GetOrderForEditQuery(_selectionStore.Id));

        if (order is null)
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("تعذر تحميل بيانات الطلب للتعديل."));
            OnRequestClose();
            return;
        }

        await EnsureClientLoadedAsync(order.ClientId);
        await EnsureDeliverymanLoadedAsync(order.DeliveryManId);
        await EnsureShippingCarrierLoadedAsync(order.ShippingCarrierId);

        foreach (OrderDeliveryStepEditDto step in order.DeliverySteps)
        {
            if (step.DeliveryMethod == DeliveryMethod.DeliveryMan)
            {
                await EnsureDeliverymanLoadedAsync(step.HandlerId);
            }
            else if (step.DeliveryMethod == DeliveryMethod.ShippingCompany)
            {
                await EnsureShippingCarrierLoadedAsync(step.HandlerId);
            }
        }

        await RunWithoutTrackingAsync(() =>
        {
            OrderBuilder.Clear();
            DeliveryMethodsViewModel.DeliverySteps.Clear();

            SelectedClient = Clients?.FirstOrDefault(client => client.Id == order.ClientId);
            SelectedPaymentMethod = PaymentMethods?.FirstOrDefault(method => method.Id == order.PaymentMothodId);
            DeliveryMethodsViewModel.SelecteddDeliveryMethod = Editor.DeliveryMethodsViewModel.DeliveryMethods
                .FirstOrDefault(method => method.Value == order.DeliveryMethod);

            DeliveryMethodsViewModel.SelectedDeliveryman = DeliveryMethodsViewModel.Deliverymen
                ?.FirstOrDefault(deliveryman => deliveryman.Id == order.DeliveryManId);

            DeliveryMethodsViewModel.SelectedShippingCarrier = DeliveryMethodsViewModel.ShippingCarriers
                ?.FirstOrDefault(carrier => carrier.Id == order.ShippingCarrierId);

            foreach (OrderItemEditDto item in order.OrderItems)
            {
                var suppliers = item.Suppliers
                    .Select(supplier => new OrderItemSupplier(supplier.Id, supplier.Name))
                    .ToArray();

                var orderItem = new OrderItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    CategoryName = item.CategoryName,
                    Price = item.UnitPrice,
                    Quantity = item.Quantity,
                    Suppliers = suppliers
                };

                orderItem.Supplier = suppliers.FirstOrDefault(supplier => supplier.Id == item.SupplierId);
                OrderBuilder.Items.Add(orderItem);
            }

            foreach (OrderDeliveryStepEditDto step in order.DeliverySteps)
            {
                var handlers = step.DeliveryMethod switch
                {
                    DeliveryMethod.DeliveryMan => DeliveryMethodsViewModel.Deliverymen
                        ?.Select(deliveryman => new Handler(deliveryman.Id, deliveryman.Name)),
                    DeliveryMethod.ShippingCompany => DeliveryMethodsViewModel.ShippingCarriers
                        ?.Select(carrier => new Handler(carrier.Id, carrier.Name)),
                    _ => Enumerable.Empty<Handler>()
                };

                var stepViewModel = new DeliveryStepViewModel
                {
                    StepOrder = step.StepOrder,
                    Method = step.DeliveryMethod,
                    Type = step.DeliveryMethod.GetDescription(),
                    Handlers = handlers?.ToArray() ?? [],
                };

                stepViewModel.SelectedHandler = stepViewModel.Handlers.FirstOrDefault(handler => handler.Id == step.HandlerId);
                DeliveryMethodsViewModel.DeliverySteps.Add(stepViewModel);
            }

            DeliveryMethodsViewModel.ResetSelectedDeliveryStepsOrder();
            HasChanges = false;
            return Task.CompletedTask;
        });
    }

    protected override async Task Save()
    {
        var order = OrderBuilder
            .WithDeliveryMethod(DeliveryMethodsViewModel.SelecteddDeliveryMethod.Value)
            .WithDeliveryman(DeliveryMethodsViewModel.SelectedDeliveryman?.Id)
            .WithShippingCarrier(DeliveryMethodsViewModel.SelectedShippingCarrier?.Id)
            .WithDeliverySteps(DeliveryMethodsViewModel.BuildDeliverySteps())
            .ForClient(SelectedClient.Id)
            .WithPaymentMethod(SelectedPaymentMethod?.Id)
            .Build();

        if (!order.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification(order.ErrorMessage));
            return;
        }

        OrderUpdateDto updateDto = new(
            _selectionStore.Id,
            order.Value.ClientId,
            order.Value.OrderStatusId,
            order.Value.DeliveryMethod,
            order.Value.DeliveryManId,
            order.Value.ShippingCarrierId,
            order.Value.OrderItems,
            order.Value.DeliverySteps,
            order.Value.PaymentMothodId);

        Result result = await _mediator.Send(new Application.Commands.OrderCommands.UpdateOrderCommand(updateDto));

        if (!result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification(result.ErrorMessage));
            return;
        }

        _messenger.Send(new Application.Messages.Orders.OrderUpdatedMessage());
        await _mediator.Publish(new Application.Notifications.SuccessNotification("تم تحديث الطلب بنجاح."));
        HasChanges = false;
        _selectionStore.Clear();
        OnRequestClose();
    }
}
