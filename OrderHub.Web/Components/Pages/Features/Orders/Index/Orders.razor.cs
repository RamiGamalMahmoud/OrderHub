using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using static OrderHub.Application.DTOs.PaymentMothodsDtos;
using Order = OrderHub.Application.Features.Orders.Queries.GetOrderPaged.Order;

namespace OrderHub.Web.Components.Pages.Features.Orders.Index;

public partial class Orders
{
    [Inject]
    private IRequestExecutor RequestExecutor { get; set; } = null!;

    private MudTable<Order>? _table;

    private IReadOnlyList<OrderDraftSummary> _draftSummaries = [];

    private string? _searchTerm;

    private bool _isDraftsOpen;

    private OrderStatus? _selectedStatus;

    private IEnumerable<EnumItem<OrderStatus>> _enumItems =
        EnumItems.For<OrderStatus>();

    private async Task OnStatusChanged(OrderStatus? status)
    {
        _selectedStatus = status;

        if (_table is not null)
            await _table.ReloadServerData();
    }
    private async Task<TableData<Order>> LoadOrdersAsync(
        TableState state,
        CancellationToken cancellationToken)
    {
        _paymentMethods = await RequestExecutor.ExecuteAsync(new Application.Queries.PaymentMothodQueries.GetPaymentMethodListQuery());
        var query =
            new Application.Features.Orders.Queries.GetOrderPaged.Query(
                PageNumber: state.Page + 1,
                PageSize: state.PageSize,
                SearchTerm: _searchTerm);

        var result = await RequestExecutor.ExecuteAsync(
            query,
            cancellationToken);

        return new TableData<Order>
        {
            Items = result.Items,
            TotalItems = result.TotalCount
        };
    }

    private async Task OnSearchChanged(string? value)
    {
        _searchTerm = value;

        if (_table is not null)
            await _table.ReloadServerData();
    }

    private async Task RefreshAsync()
    {
        if (_table is not null)
            await _table.ReloadServerData();
    }

    private void SelectOrder(Order order)
    {
    }

    private void ShowDrafts()
    {
        _isDraftsOpen = !_isDraftsOpen;
    }

    private int? _selectedPaymentMethodId = null;
    private IEnumerable<PaymentMethodListDto?> _paymentMethods = [];
}
