using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Features.Setup.PaymentMethods.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Common;
using OrderHub.Web.Components.Pages.Features.Orders.Workflow;
using Order = OrderHub.Application.Features.Orders.GetPage.OrderListItem;

namespace OrderHub.Web.Components.Pages.Features.Orders.Index;

public partial class Orders : IDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    [Inject] private IRequestExecutor RequestExecutor { get; set; } = null!;

    [Inject] private OrderHub.Web.Services.PermissionAccess PermissionAccess { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "page")]
    public int? Page { get; set; }

    private MudTable<Order>? _table;
    private bool _archivedOnly;
    private int _pageIndex = 1;
    private int _pageSize = 10;
    private int? _totalItems;
    private string? _searchTerm;
    private bool _isDraftsOpen;
    private OrderStatus? _selectedStatus;
    private int? _selectedPaymentMethodId;
    private IEnumerable<PaymentMethodListItem?> _paymentMethods = [];

    private IEnumerable<EnumItem<OrderStatus>> _enumItems = EnumItems.For<OrderStatus>();

    private bool HasFilters =>
        !string.IsNullOrWhiteSpace(_searchTerm) || _selectedStatus.HasValue || _selectedPaymentMethodId.HasValue;

    private PageScaffold.CrumbItem[] _crumbs => _archivedOnly
        ? [new("الطلبات", "/orders"), new("سجل المؤرشف")]
        : [new("الطلبات")];

    protected override void OnInitialized()
    {
        _archivedOnly = IsArchivedRoute(Navigation.Uri);
        Navigation.LocationChanged += OnLocationChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        var archived = IsArchivedRoute(Navigation.Uri);
        var requestedPage = Math.Max(1, Page ?? 1);

        if (_archivedOnly == archived && _pageIndex == requestedPage)
            return;

        _archivedOnly = archived;
        _pageIndex = requestedPage;

        if (_table is not null)
            await _table.ReloadServerData();
    }

    /// <summary>Both routes reuse this component, so OnParametersSetAsync is not enough to repaint on a route switch.</summary>
    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        var archived = IsArchivedRoute(args.Location);

        _ = InvokeAsync(async () =>
        {
            if (_archivedOnly == archived)
                return;

            _archivedOnly = archived;
            _pageIndex = 1;
            _totalItems = null;
            StateHasChanged();

            if (_table is not null)
                await _table.ReloadServerData();
        });
    }

    public void Dispose()
    {
        Navigation.LocationChanged -= OnLocationChanged;
    }

    private static bool IsArchivedRoute(string uri) =>
        new Uri(uri).AbsolutePath.TrimEnd('/').EndsWith("/archived", StringComparison.OrdinalIgnoreCase);

    private async Task<TableData<Order>> LoadOrdersAsync(TableState state, CancellationToken cancellationToken)
    {
        _paymentMethods = await PermissionAccess.IsAllowedAsync(PermissionFeature.PaymentMethods, PermissionLevel.Read, cancellationToken)
            ? await RequestExecutor.ExecuteAsync(new Application.Features.Setup.PaymentMethods.GetPaymentMethodListQuery(), cancellationToken)
            : [];

        var query = new Application.Features.Orders.GetPage.GetOrdersPageQuery(
            PageNumber: _pageIndex,
            PageSize: _pageSize,
            SearchTerm: _searchTerm,
            PaymentMethodId: _selectedPaymentMethodId,
            OrderStatus: _selectedStatus,
            ArchivedOnly: _archivedOnly);

        var result = await RequestExecutor.ExecuteAsync(query, cancellationToken);
        _totalItems = result.TotalCount;

        return new TableData<Order>
        {
            Items = result.Items,
            TotalItems = result.TotalCount
        };
    }

    private async Task OnSearchChanged(string? value)
    {
        _searchTerm = value;
        await ReloadFirstPageAsync();
    }

    private async Task OnStatusChipAsync(OrderStatus? status)
    {
        if (_selectedStatus == status)
            return;

        _selectedStatus = status;
        await ReloadFirstPageAsync();
    }

    private async Task OnPaymentChanged(int? value)
    {
        _selectedPaymentMethodId = value;
        await ReloadFirstPageAsync();
    }

    private async Task ReloadFirstPageAsync()
    {
        _pageIndex = 1;
        if (Page.HasValue)
            SyncPageQuery();
        if (_table is not null)
            await _table.ReloadServerData();
    }

    private async Task OnFooterPageChanged(int page)
    {
        _pageIndex = Math.Max(1, page);
        SyncPageQuery();
        if (_table is not null)
            await _table.ReloadServerData();
    }

    private async Task OnFooterRowsPerPageChanged(int rowsPerPage)
    {
        _pageSize = rowsPerPage;
        _pageIndex = 1;
        SyncPageQuery();
        if (_table is not null)
            await _table.ReloadServerData();
    }

    private void SyncPageQuery()
    {
        var uri = Navigation.GetUriWithQueryParameter(
            "page",
            _pageIndex == 1 ? null : _pageIndex.ToString(CultureInfo.InvariantCulture));

        Navigation.NavigateTo(uri, replace: true);
    }

    private async Task RefreshAsync()
    {
        if (_table is not null)
            await _table.ReloadServerData();
    }

    private void ShowDrafts() => _isDraftsOpen = !_isDraftsOpen;

    private string StatusChipClass(OrderStatus? status)
    {
        var active = _selectedStatus == status;
        var tone = active
            ? status.HasValue
                ? $"{OrderWorkflowPresentation.Tag(status.Value)} shadow-sm"
                : "border-brand-200 bg-brand-50 text-brand-700 shadow-sm"
            : "border-slate-200 bg-white text-slate-600 hover:bg-slate-50";

        return $"inline-flex items-center gap-1.5 rounded-full border px-3 py-1.5 text-xs font-bold transition-colors {tone}";
    }
}
