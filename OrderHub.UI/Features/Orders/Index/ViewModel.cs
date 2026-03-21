using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Domain.Common;
using OrderHub.UI.Common;
using OrderHub.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.DTOs.OrderDtos;
using static OrderHub.Application.DTOs.PaymentMothodsDtos;

namespace OrderHub.UI.Features.Orders.Index;

internal partial class ViewModel : IndexViewModelBase<OrderViewModel>
{
    private readonly IDialogService _dialogService;

    private readonly ObservableCollection<OrderViewModel> _orders = new();
    public ObservableCollection<OrderViewModel> Orders => _orders;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private IEnumerable<PaymentMethodListDto> _paymentMethods;

    [ObservableProperty]
    private string _searchTerm;

    private CancellationTokenSource _searchCts;

    public ViewModel(IMediator mediator, IMessenger messenger, IDialogService dialogService)
        : base(mediator, messenger)
    {
        _dialogService = dialogService;

        messenger.Register<Application.Messages.Orders.OrderCreatedMessage>(
            this,
            async (_, _) => await ReloadAsync());
    }

    // =========================
    // 🔄 Lifecycle
    // =========================

    protected override async Task LoadAsync()
    {
        try
        {
            IsLoading = true;

            PaymentMethods = await _mediator.Send(
                new Application.Queries.PaymentMothodQueries.GetPaymentMethodListQuery());

            var orders = await _mediator.Send(
                new Application.Queries.OrderQueries.GetOrdersQuery());

            UpdateOrders(orders.Select(Map));
        }
        catch (Exception ex)
        {
            await _mediator.Publish(
                new Application.Notifications.AppliationNotification(ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override Task ReloadAsync() => LoadAsync();

    // =========================
    // 🔍 Debounced Search
    // =========================

    async partial void OnSearchTermChanged(string oldValue, string newValue)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            //await Task.Delay(400, token);

            IsLoading = true;

            var result = await _mediator.Send(
                new Application.Queries.OrderQueries.GetClientOrdersQuery(newValue),
                token);

            UpdateOrders(result.Select(Map));
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            await _mediator.Publish(
                new Application.Notifications.AppliationNotification(ex.Message));
        }
        finally
        {
            IsLoading = false;
        }
    }


    protected override Task ShowCreateAsync()
    {
        _dialogService.ShowDialog<Create.View>();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task BroadcastOrder(OrderViewModel model)
    {
        Result result = await _mediator.Send(
            new Application.Commands.OrderCommands.BroadcastOrderStatusCommand(model.Id));

        if (!result.IsSuccess)
        {
            await _mediator.Publish(
                new Application.Notifications.AppliationNotification(result.ErrorMessage));
        }
    }

    [RelayCommand]
    private async Task BroadcastAllOrders()
    {
        try
        {
            await Task.WhenAll(
                Orders.Select(order =>
                    _mediator.Send(
                        new Application.Commands.OrderCommands.BroadcastOrderStatusCommand(order.Id)))
            );
        }
        catch (Exception ex)
        {
            await _mediator.Publish(
                new Application.Notifications.AppliationNotification(ex.Message));
        }
    }

    protected override Task ShowEditAsync(OrderViewModel model) => Task.CompletedTask;

    protected override Task DeleteAsync(OrderViewModel model) => Task.CompletedTask;


    private void UpdateOrders(IEnumerable<OrderViewModel> newOrders)
    {
        _orders.Clear();
        foreach (var order in newOrders)
        {
            _orders.Add(order);
        }
    }

    private OrderViewModel Map(OrderListDto o) => new(
        _mediator,
        o.Id,
        o.OrderNumber,
        o.ClientName,
        o.ClientPhoneNumber,
        o.ItemsCount,
        o.Total,
        o.CreatedAt,
        o.PaymentMethod,
        o.EnumItem
    );
}