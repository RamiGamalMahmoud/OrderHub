using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Features.Orders.Editor;
using OrderHub.UI.Interfaces;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.OrderDtos;

namespace OrderHub.UI.Features.Orders.Create;

internal class ViewModel : Editor.ViewModel
{
    public ViewModel(IMediator mediator, IMessenger messenger, IDialogService dialogService) : base(mediator, dialogService, messenger)
    {
        HasChanges = true;
    }

    public override string Title => "إنشاء طلب جديد";

    public override bool CanSave => base.CanSave && OrderBuilder.ItemsCount > 0;

    protected override async Task Save()
    {
        OrderCreateDto order = OrderBuilder
            .WithDeliveryMethod(DeliveryMethodsViewModel.SelecteddDeliveryMethod.Value)
            .WithDeliveryman(DeliveryMethodsViewModel.SelectedDeliveryman?.Id)
            .WithShippingCarrier(DeliveryMethodsViewModel.SelectedShippingCarrier?.Id)
            .ForClient(SelectedClient.Id)
            .WithPaymentMethod(SelectedPaymentMethod?.Id)
            .Build();

        Result<int> result = await _mediator.Send(new Application.Commands.OrderCommands.CreateOrderCommand(order));
        if(result.IsSuccess)
        {
            _messenger.Send(new Application.Messages.Orders.OrderCreatedMessage());
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم انشاء الطلب بنجاح"));
            await _mediator.Publish(new Application.Notifications.SuccessNotification("جاري ارسال الإشعارات"));
            await _mediator.Send(new Application.Commands.OrderCommands.BroadcastOrderStatusCommand(result.Value));
            OrderCreated = true;
            //OnRequestClose();
        }
        else
        {

        }
    }
}
