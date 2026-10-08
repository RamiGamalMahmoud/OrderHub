using OrderHub.UI.Messaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.UI.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;

namespace OrderHub.UI.Features.Clients.Index;

public partial class ViewModel : IndexViewModelBase<ClientListDto>
{
    private List<ClientListDto> _allClients = [];

    public ViewModel(IMediator mediator) : base(mediator)
    {
        WeakReferenceMessenger.Default.Register<ClientsMessages.ClientCreatedMessage>(this, async (r, m) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<ClientsMessages.ClientUpdatedMessage>(this, async (r, m) => await ReloadAsync());
        WeakReferenceMessenger.Default.Register<ClientsMessages.ClientDeletedMessage>(this, async (r, m) => await ReloadAsync());
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
