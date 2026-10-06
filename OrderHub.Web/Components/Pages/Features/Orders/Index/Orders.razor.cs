using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Application.Features.Setup.PaymentMethods.Contracts;
using Order = OrderHub.Application.Features.Orders.GetPage.OrderListItem;

namespace OrderHub.Web.Components.Pages.Features.Orders.Index;

public partial class Orders
{
    [Inject]
    private IRequestExecutor RequestExecutor { get; set; } = null!;

    [Inject]
    private OrderHub.Web.Services.PermissionAccess PermissionAccess { get; set; } = null!;

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
        _paymentMethods = await PermissionAccess.IsAllowedAsync(PermissionFeature.PaymentMethods, PermissionLevel.Read, cancellationToken)
            ? await RequestExecutor.ExecuteAsync(new Application.Features.Setup.PaymentMethods.GetPaymentMethodListQuery(), cancellationToken)
            : [];
        var query =
            new Application.Features.Orders.GetPage.GetOrdersPageQuery(
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
    private IEnumerable<PaymentMethodListItem?> _paymentMethods = [];
}
