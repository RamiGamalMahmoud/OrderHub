using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.Queries;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using OrderHub.UI.Features.Orders.Editor;
using OrderHub.UI.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders.Edit;

internal class ViewModel : Editor.ViewModel, IParameterizedViewModel
{
    private readonly IOrderStore _orderStore;
    private int _id;

    public ViewModel(
        IMediator mediator,
        IDialogService dialogService,
        IMessenger messenger,
        IProductStore productStore,
        IOrderStore orderStore,
        ILookupService lookupService) : base(mediator, dialogService, messenger, productStore, lookupService)
    {
        _orderStore = orderStore;
    }

    public override string ActionName => "حفظ التعديلات";
    public override string Title => "تعديل طلب";

    protected override async Task AfterLoadAsync()
    {
        GetOrderEdit.Order order = await _mediator.Send(new Application.Features.Orders.Queries.GetOrderEdit.Query(_id));

        if (order is null)
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification("تعذر تحميل بيانات الطلب للتعديل."));
            OnRequestClose();
            return;
        }

        await EnsureDeliverymanLoadedAsync(order.DeliveryManId);
        await EnsureShippingCarrierLoadedAsync(order.ShippingCarrierId);

        foreach (var step in order.DeliverySteps)
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
            DeliveryMethodsViewModel.DeliverySteps.Clear();

            PartyPanel.SelectedClient = PartyPanel.Clients?.FirstOrDefault(client => client.Id == order.ClientId);
            PartyPanel.SelectedPaymentMethod = PartyPanel.PaymentMethods?.FirstOrDefault(method => method.Id == order.PaymentMothodId);
            DeliveryMethodsViewModel.SelecteddDeliveryMethod = Editor.DeliveryMethodsViewModel.DeliveryMethods
                .FirstOrDefault(method => method.Value == order.DeliveryMethod);

            DeliveryMethodsViewModel.SelectedDeliveryman = DeliveryMethodsViewModel.Deliverymen
                ?.FirstOrDefault(deliveryman => deliveryman.Id == order.DeliveryManId);

            DeliveryMethodsViewModel.SelectedShippingCarrier = DeliveryMethodsViewModel.ShippingCarriers
                ?.FirstOrDefault(carrier => carrier.Id == order.ShippingCarrierId);

            foreach (GetOrderEdit.OrderItem item in order.OrderItems)
            {
                var suppliers = item.Suppliers
                    .Select(supplier => new OrderItemSupplier(supplier.Id, supplier.Name))
                    .ToArray();

                OrderItemViewModel orderItem = new()
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    CategoryName = item.CategoryName,
                    Price = item.UnitPrice,
                    Quantity = item.Quantity,
                    Suppliers = suppliers,
                    Supplier = suppliers.FirstOrDefault(supplier => supplier.Id == item.SupplierId),
                    Properties = item.Properties.Select(property => new OrderItemProperty(
                        property.PropertyId,
                        property.Name,
                        property.IsRequired,
                        property.PropertyType,
                        property.Options.Select(option => new OrderItemPropertyOption(option.OptionId, option.Value))
                        .ToList(),
                        property.SelectedValue))
                    .ToList()
                };
                orderItem.StateChanged += OrderItem_StateChanged;
                Items.Add(orderItem);
            }

            foreach (GetOrderEdit.DeliveryStep step in order.DeliverySteps)
            {
                var handlers = step.DeliveryMethod switch
                {
                    DeliveryMethod.DeliveryMan => DeliveryMethodsViewModel.Deliverymen
                        ?.Select(deliveryman => new Handler(deliveryman.Id, deliveryman.Name)),
                    DeliveryMethod.ShippingCompany => DeliveryMethodsViewModel.ShippingCarriers
                        ?.Select(carrier => new Handler(carrier.Id, carrier.Name)),
                    _ => Enumerable.Empty<Handler>()
                };

                DeliveryStepViewModel stepViewModel = new()
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

    private void OrderItem_StateChanged(object sender, System.EventArgs e)
    {
        SaveCommand.NotifyCanExecuteChanged();
    }

    protected override async Task Save()
    {
        Result result = await _orderStore.UpdateOrder(_id, BuildOrder());
        if(result.IsSuccess)
        {
            _messenger.Send(new Application.Messages.Orders.OrderUpdatedMessage());
        }
        OnRequestClose();
    }

    public void Initialize(object parameter)
    {
        if(parameter is int id)
        {
            _id = id;
        }
    }
}
