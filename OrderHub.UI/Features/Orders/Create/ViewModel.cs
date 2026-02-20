using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Interfaces;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.Orders.Create;

internal class ViewModel : Editor.ViewModel
{
    private readonly IMessenger _messenger;

    public ViewModel(IMediator mediator, IMessenger messenger, IDialogService dialogService) : base(mediator, dialogService, messenger)
    {
        _messenger = messenger;
    }

    public override string Title => "إنشاء طلب جديد";

    public override bool CanSave => !HasErrors && OrderBuilder.Count > 0;

    protected override async Task Save()
    {
        Result result = await _mediator.Send(new Application.Commands.OrderCommands.CreateOrderCommand(OrderBuilder.Build(SelectedClient)));
        if(result.IsSuccess)
        {
            _messenger.Send(new Application.Messages.Orders.OrderCreatedMessage());
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم انشاء الطلب بنجاح"));
            OnRequestClose();
        }
        else
        {

        }
    }
}
