using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Commands;
using OrderHub.UI.Common;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.WhatsappGroups.Create;

public class ViewModel : Editor.ViewModel
{
    private readonly IMessenger _messenger;

    public ViewModel(IMediator mediator, IMessenger messenger) : base(mediator)
    {
        _messenger = messenger;
        HasChanges = true;
    }

    public override string Title => "إنشاء مجموعة واتساب";

    protected override async Task Save()
    {
        var result = await _mediator.Send(new WhatsappGroupCommands.CreateWhatsappGroupCommand(Name, GroupType.Value, GroupLink));
        if (result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification(MessageBuilder.Build(MessageBuilder.OperationType.Create, true, "مجموعة الواتساب")));
            _messenger.Send(new Application.Messages.WhatsappGroups.WhatsappGroupCreatedMessage());
            OnRequestClose();
        }
        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification(MessageBuilder.Build(MessageBuilder.OperationType.Create, false, "مجموعة الواتساب")));
        }
    }
}
