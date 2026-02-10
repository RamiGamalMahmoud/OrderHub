using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;

namespace OrderHub.UI.Features.Clients.Index;

public partial class ViewModel : IndexViewModelBase<ClientListDto>
{
    private readonly IDialogService _dialogService;
    private readonly ISelectionStore<IClientMarker, int> _selectionStore;

    public ViewModel(IMediator mediator, IDialogService dialogService, ISelectionStore<IClientMarker, int> selectionStore, IMessenger messenger) : base(mediator, messenger)
    {
        _dialogService = dialogService;
        _selectionStore = selectionStore;

        _messenger.Register<Application.Messages.Clients.ClientCreatedMessage>(this, async (r, m) => await ReloadAsync());
        _messenger.Register<Application.Messages.Clients.ClientUpdatedMessage>(this, async (r, m) => await ReloadAsync());
        _messenger.Register<Application.Messages.Clients.ClientDeletedMessage>(this, async (r, m) => await ReloadAsync());
    }

    protected override async Task DeleteAsync(ClientListDto dto)
    {
        if (!_dialogService.Confirm($"هل تريد حذف العميل ( {dto.Name} )؟"))
            return;
        Result result = await _mediator.Send(new Application.Commands.ClienCommands.DeleteClientCommand(dto.Id));
        if (result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم حذف العميل."));
            _messenger.Send(new Application.Messages.Clients.ClientDeletedMessage(dto.Id));
        }

        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification(result.ErrorMessage));
        }
    }

    protected override async Task LoadAsync()
    {
        Clients = await _mediator.Send(new Application.Queries.ClientQueries.GetAllClientsQuery());
    }

    protected override async Task ReloadAsync()
    {
        Clients = await _mediator.Send(new Application.Queries.ClientQueries.GetAllClientsQuery());
    }

    protected override Task ShowEditAsync(ClientListDto dto)
    {
        _selectionStore.Id = dto.Id;
        _dialogService.ShowDialog<Update.View>();
        return Task.CompletedTask;
    }

    protected override Task ShowCreateAsync()
    {
        _dialogService.ShowDialog<Create.View>();
        return Task.CompletedTask;
    }

    [ObservableProperty]
    private IEnumerable<ClientListDto> _clients;
}
