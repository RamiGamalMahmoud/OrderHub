using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using OrderHub.UI.Stores.Markers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;

namespace OrderHub.UI.Features.Clients.Index;

public partial class ViewModel : IndexViewModelBase<ClientListDto>
{
    private readonly ISelectionStore<IClientMarker, int> _selectionStore;
    private List<ClientListDto> _allClients = [];

    public ViewModel(IMediator mediator, ISelectionStore<IClientMarker, int> selectionStore) : base(mediator)
    {
        _selectionStore = selectionStore;

        WeakReferenceMessenger.Default.Register<Application.Messages.Clients.ClientCreatedMessage>(this, async (r, m) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.Clients.ClientUpdatedMessage>(this, async (r, m) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<Application.Messages.Clients.ClientDeletedMessage>(this, async (r, m) => await ReloadAsync());
    }

    protected override async Task DeleteAsync(ClientListDto dto)
    {
        if (!DialogService.Instance.Confirm($"هل تريد حذف العميل ( {dto.Name} )؟"))
            return;
        Result result = await _mediator.Send(new Application.Commands.ClienCommands.DeleteClientCommand(dto.Id));
        if (result.IsSuccess)
        {
            await _mediator.Publish(new Application.Notifications.SuccessNotification("تم حذف العميل."));
            WeakReferenceMessenger.Default.Send(new Application.Messages.Clients.ClientDeletedMessage(dto.Id));
        }

        else
        {
            await _mediator.Publish(new Application.Notifications.ErrorNotification(result.ErrorMessage));
        }
    }

    protected override async Task LoadAsync()
    {
        _allClients = (await _mediator.Send(new Application.Queries.ClientQueries.GetAllClientsQuery())).ToList();
        ApplyFilter();
    }

    protected override async Task ReloadAsync()
    {
        _allClients = (await _mediator.Send(new Application.Queries.ClientQueries.GetAllClientsQuery())).ToList();
        ApplyFilter();
    }

    protected override Task ShowEditAsync(ClientListDto dto)
    {
        _selectionStore.Id = dto.Id;
        DialogService.Instance.ShowDialog<Update.View>();
        return Task.CompletedTask;
    }

    protected override Task ShowCreateAsync()
    {
        DialogService.Instance.ShowDialog<Create.View>();
        return Task.CompletedTask;
    }

    [ObservableProperty]
    private IEnumerable<ClientListDto> _clients;

    [ObservableProperty]
    private string _searchTerm;

    partial void OnSearchTermChanged(string oldValue, string newValue) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<ClientListDto> filtered = _allClients;

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            string term = SearchTerm.Trim();
            filtered = filtered.Where(client =>
                client.Name?.Contains(term) == true
                || client.Address?.Contains(term) == true
                || client.PhoneNumber?.Contains(term) == true);
        }

        Clients = filtered.ToList();
    }
}
