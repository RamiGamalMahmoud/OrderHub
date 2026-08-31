using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Commands;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.Features.WhatsappGroups.Create;

public class ViewModel : Editor.ViewModel
{
    public ViewModel(IMediator mediator) : base(mediator)
    {
        HasChanges = true;
    }

    public override string Title => "إنشاء مجموعة واتساب";

    protected override async Task Save()
    {
        var result = await _mediator.Send(new WhatsappGroupCommands.CreateWhatsappGroupCommand(Name, GroupType.Value, GroupLink));
        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess(MessageBuilder.Build(MessageBuilder.OperationType.Create, true, "مجموعة الواتساب"));
            WeakReferenceMessenger.Default.Send(new Application.Messages.WhatsappGroups.WhatsappGroupCreatedMessage());
            OnRequestClose();
        }
        else
        {
            NotificationService.Instance.ShowError(MessageBuilder.Build(MessageBuilder.OperationType.Create, false, "مجموعة الواتساب"));
        }
    }
}
