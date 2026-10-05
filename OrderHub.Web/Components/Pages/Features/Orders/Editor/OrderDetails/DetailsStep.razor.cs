using Microsoft.AspNetCore.Components;
using OrderHub.Application.Features.Orders.GetEditorLookups;
using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.OrderDetails;

public partial class DetailsStep : IDisposable
{
    [Parameter, EditorRequired]
    public OrderEditorState State { get; set; } = null!;

    [Parameter]
    public EventCallback DetailsChanged { get; set; }

    private IReadOnlyList<ClientLookup> _clients = [];
    private IReadOnlyList<PaymentMethodLookup> _payments = [];
    private IReadOnlyList<DeliverymanLookup> _deliverymen = [];
    private IReadOnlyList<ShippingCarrierLookup> _carriers = [];

    private bool _loading;
    private bool _disposed;
    private string? _error;

    protected override Task OnInitializedAsync()
    {
        return LoadLookupsAsync();
    }

    private async Task LoadLookupsAsync()
    {
        if (_loading || _disposed)
            return;

        _loading = true;
        _error = null;

        try
        {
            var lookups = await RequestExecutor.ExecuteAsync(new GetOrderEditorLookupsQuery());
            var clients = lookups.Clients.ToArray();

            var payments = lookups.PaymentMethods
                .Where(payment =>
                    payment.IsActive ||
                    payment.Id == State.PaymentMethodId)
                .ToArray();

            var deliverymen = lookups.Deliverymen
                .ToArray();

            var carriers = lookups.ShippingCarriers
                .ToArray();

            if (_disposed)
                return;

            _clients = clients;
            _payments = payments;
            _deliverymen = deliverymen;
            _carriers = carriers;
        }
        catch (Exception exception)
        {
            Logger.LogError(
                exception,
                "Failed to load order detail lookups.");

            if (!_disposed)
            {
                _error =
                    "تعذر تحميل الاختيارات. بيانات الطلب محفوظة في المحرر؛ حاول مرة أخرى.";
            }
        }
        finally
        {
            _loading = false;
        }
    }

    private Task SetClientAsync(int? value)
    {
        return ChangeAsync(
            State.ClientId,
            value,
            newValue =>
            {
                State.ClientId = newValue;
                State.ClientName = _clients.FirstOrDefault(client => client.Id == newValue)?.Name;
            });
    }

    private Task SetPaymentAsync(int? value)
    {
        return ChangeAsync(
            State.PaymentMethodId,
            value,
            newValue => State.PaymentMethodId = newValue);
    }

    private Task SetSourceAsync(OrderSource? value)
    {
        return ChangeAsync(
            State.OrderSource,
            value,
            newValue => State.OrderSource = newValue);
    }

    private Task ChangeAsync<T>(
        T current,
        T value,
        Action<T> apply)
    {
        if (_disposed ||
            EqualityComparer<T>.Default.Equals(current, value))
        {
            return Task.CompletedTask;
        }

        apply(value);

        return DetailsChanged.InvokeAsync();
    }

    private Task NotifyChangedAsync()
    {
        if (_disposed)
            return Task.CompletedTask;

        return DetailsChanged.InvokeAsync();
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
