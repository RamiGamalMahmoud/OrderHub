using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.WhatsappGroupDtos;

namespace OrderHub.UI.Features.WhatsappGroups.Update;

public partial class ViewModel(IMediator mediator) : Editor.ViewModel(mediator), IParameterizedViewModel
{
    private int _groupId;

    public override string Title => "تعديل مجموعة واتساب";

    public Task Initialize(object parameter)
    {
        if(parameter is not null && parameter is int id)
        {
            _groupId = id;
        }
        return Task.CompletedTask;
    }

    public async Task LoadAsync()
    {
        WhatsappGroupEditDto whatsappGroupEditDto = await _mediator.Send(new Application.Queries.WhatsappGroupQueries.GetAllWhatsappGroupForEditQuery(_groupId));

        Name = whatsappGroupEditDto.Name;
        GroupType = new EnumItem<WhatsappGroupType>(whatsappGroupEditDto.WhatsappGroupType, whatsappGroupEditDto.WhatsappGroupType.GetDescription());
        GroupLink = whatsappGroupEditDto.GroupLink;
        HasChanges = false;
    }

    protected override async Task Save()
    {
        Result result = await _mediator.Send(new Application.Commands.WhatsappGroupCommands.UpdateWhatsappGroupCommand(_groupId, Name, GroupType.Value, GroupLink));
        if(result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess(MessageBuilder.Build(MessageBuilder.OperationType.Update, true, "مجموعة الواتساب"));
            WeakReferenceMessenger.Default.Send(new Application.Messages.WhatsappGroups.WhatsappGroupUpdatedMessage());
            OnRequestClose();
        }
        else
        {
            NotificationService.Instance.ShowError(MessageBuilder.Build(MessageBuilder.OperationType.Update, false, "مجموعة الواتساب"));
        }
    }
}
