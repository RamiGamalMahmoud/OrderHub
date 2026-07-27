using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Domain.Common;
using OrderHub.UI.Features.Orders.Editor;
using OrderHub.UI.Interfaces;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.OrderDtos;

namespace OrderHub.UI.Features.Orders.Create;

internal class ViewModel : Editor.ViewModel
{
    public ViewModel(IMediator mediator, IMessenger messenger, IDialogService dialogService, IProductStore productStore)
        : base(mediator, dialogService, messenger, productStore)
    {
        HasChanges = true;
    }

    public override string Title => "إنشاء طلب جديد";
    public override string ActionName => "إتمام الطلب";

    public override bool CanSave => base.CanSave && OrderBuilder.ItemsCount > 0;

    protected override async Task Save()
    {
        OrderCreateDto order = OrderBuilder
            .WithDeliveryMethod(DeliveryMethodsViewModel.SelecteddDeliveryMethod.Value)
            .WithDeliveryman(DeliveryMethodsViewModel.SelectedDeliveryman?.Id)
            .WithShippingCarrier(DeliveryMethodsViewModel.SelectedShippingCarrier?.Id)
            .WithDeliverySteps(DeliveryMethodsViewModel.BuildDeliverySteps())
            .ForClient(PartyPanel.SelectedClient.Id)
            .WithPaymentMethod(PartyPanel.SelectedPaymentMethod?.Id)
            .Build().Value;

        Result<int> result = await _mediator.Send(new Application.Commands.OrderCommands.CreateOrderCommand(order));

        if (result.IsSuccess)
        {
            await HandleOrderSuccess(result.Value);
            OnRequestClose();
        }
        else
        {
            await HandleOrderFailure();
        }
    }

    private async Task HandleOrderSuccess(int orderId)
    {
        _messenger.Send(new Application.Messages.Orders.OrderCreatedMessage());

        await PublishSuccessNotification("تم انشاء الطلب بنجاح");
    }

    private async Task HandleOrderFailure()
    {
        await _mediator.Publish(new Application.Notifications.ErrorNotification("حدث خطأ أثناء إنشاء الطلب"));
    }

    private async Task PublishSuccessNotification(string message)
    {
        await _mediator.Publish(new Application.Notifications.SuccessNotification(message));
    }
}
