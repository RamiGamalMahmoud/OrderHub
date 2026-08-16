using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Common.Extensions;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.WhatsappGroupDtos;

namespace OrderHub.UI.Features.WhatsappGroups.Update;

public partial class ViewModel : Editor.ViewModel
{
    private readonly ISelectionStore<IWhatsappGroupMarker, int> _selectionStore;

    public ViewModel(IMediator mediator, ISelectionStore<IWhatsappGroupMarker, int> selectionStore) : base(mediator)
    {
        _selectionStore = selectionStore;
    }

    public override string Title => "تعديل مجموعة واتساب";

    public async Task LoadAsync()
    {
        WhatsappGroupEditDto whatsappGroupEditDto = await _mediator.Send(new Application.Queries.WhatsappGroupQueries.GetAllWhatsappGroupForEditQuery(_selectionStore.Id));

        Name = whatsappGroupEditDto.Name;
        GroupType = new EnumItem<WhatsappGroupType>(whatsappGroupEditDto.WhatsappGroupType, whatsappGroupEditDto.WhatsappGroupType.GetDescription());
        GroupLink = whatsappGroupEditDto.GroupLink;
        HasChanges = false;
    }

    protected override async Task Save()
    {
        Result result = await _mediator.Send(new Application.Commands.WhatsappGroupCommands.UpdateWhatsappGroupCommand(_selectionStore.Id, Name, GroupType.Value, GroupLink));
        if(result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification(MessageBuilder.Build(MessageBuilder.OperationType.Update, true, "مجموعة الواتساب")));
            WeakReferenceMessenger.Default.Send(new Application.Messages.WhatsappGroups.WhatsappGroupUpdatedMessage());
            _selectionStore.Clear();
            OnRequestClose();
        }
        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification(MessageBuilder.Build(MessageBuilder.OperationType.Update, false, "مجموعة الواتساب")));
        }
    }
}
