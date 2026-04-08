using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using OrderHub.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.ClientDtos;
using static OrderHub.Application.DTOs.PaymentMothodsDtos;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class OrderPartyPanelViewModel : ObservableValidator
{
    private readonly IMediator _mediator;
    private readonly IDialogService _dialogService;
    private CancellationTokenSource _clientSearchCts;

    public OrderPartyPanelViewModel(IMediator mediator, IDialogService dialogService)
    {
        _mediator = mediator;
        _dialogService = dialogService;
        ValidateAllProperties();
    }

    [ObservableProperty]
    private IEnumerable<ClientListDto> _clients;

    [ObservableProperty]
    private IEnumerable<PaymentMethodListDto> _paymentMethods;

    [ObservableProperty]
    private PaymentMethodListDto _selectedPaymentMethod;

    [ObservableProperty]
    private string _clientSearchTerm;

    [ObservableProperty]
    [Required(ErrorMessage = "العميل مطلوب")]
    [NotifyDataErrorInfo]
    private ClientListDto _selectedClient;

    public async Task LoadAsync()
    {
        var paymentsTask = _mediator.Send(new Application.Queries.PaymentMothodQueries.GetPaymentMethodListQuery());
        var clientsTask = _mediator.Send(new Application.Queries.ClientQueries.GetClientsByNameQuery());

        await Task.WhenAll(paymentsTask, clientsTask);

        PaymentMethods = await paymentsTask;
        Clients = MergeSelectedItem(await clientsTask, SelectedClient);
    }

    public async Task ReloadClientsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        IEnumerable<ClientListDto> clients = await _mediator.Send(
            new Application.Queries.ClientQueries.GetClientsByNameQuery(searchTerm),
            cancellationToken);

        Clients = MergeSelectedItem(clients, SelectedClient);
    }

    public async Task EnsureClientLoadedAsync(int clientId)
    {
        if (Clients?.Any(client => client.Id == clientId) == true)
            return;

        ClientListDto client = await _mediator.Send(new Application.Queries.ClientQueries.GetClientByIdQuery(clientId));
        if (client is not null)
        {
            Clients = MergeSelectedItem(Clients, client);
        }
    }

    [RelayCommand]
    private void ShowCreateClient()
        => _dialogService.ShowDialog<Features.Clients.Create.View>();

    async partial void OnClientSearchTermChanged(string oldValue, string newValue)
    {
        _clientSearchCts?.Cancel();
        _clientSearchCts = new CancellationTokenSource();
        CancellationToken token = _clientSearchCts.Token;

        try
        {
            await Task.Delay(300, token);
            await ReloadClientsAsync(newValue, token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static IEnumerable<TItem> MergeSelectedItem<TItem>(IEnumerable<TItem> items, TItem selectedItem)
        where TItem : class
    {
        TItem[] results = (items ?? Enumerable.Empty<TItem>()).ToArray();

        if (selectedItem is null || results.Any(item => EqualityComparer<TItem>.Default.Equals(item, selectedItem)))
            return results;

        return new[] { selectedItem }.Concat(results);
    }
}
