using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Services;
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
            NotificationService.Instance.ShowSuccess("تم حذف العميل.");
            WeakReferenceMessenger.Default.Send(new Application.Messages.Clients.ClientDeletedMessage(dto.Id));
        }

        else
        {
            NotificationService.Instance.ShowError(result.ErrorMessage);
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

    protected override async Task ShowEditAsync(ClientListDto dto)
    {
        await DialogService.Instance.ShowDialog<Update.View>("تعديل بيانات عميل", dto.Id);
    }

    protected override async Task ShowCreateAsync()
    {
        await DialogService.Instance.ShowDialog<Create.View>("إضافة عميل جديد");
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
