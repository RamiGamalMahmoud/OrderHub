using Microsoft.AspNetCore.Components;
using OrderHub.Application.Common.Lookups;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.OrderDetails;

public partial class DetailsStep : IDisposable
{
    [Parameter, EditorRequired] public OrderEditorState State { get; set; } = null!;
    [Parameter] public EventCallback DetailsChanged { get; set; }
    [Inject] private ILookupService Lookups { get; set; } = null!;
    [Inject] private ILogger<DetailsStep> Logger { get; set; } = null!;
    private IReadOnlyList<ClientLookup> _clients = [];
    private IReadOnlyList<PaymentMethodLookup> _payments = [];
    private IReadOnlyList<DeliverymanLookup> _deliverymen = [];
    private IReadOnlyList<ShippingCarrierLookup> _carriers = [];
    private bool _loading;
    private bool _disposed;
    private string? _error;

    protected override Task OnInitializedAsync() => LoadLookupsAsync();

    private async Task LoadLookupsAsync()
    {
        if (_loading || _disposed)
            return;
        _loading = true;
        _error = null;
        try
        {
            var clients = (await Lookups.GetClients()).ToArray();
            var payments = (await Lookups.GetPaymentMethods()).Where(p => p.IsActive || p.Id == State.PaymentMethodId).ToArray();
            var deliverymen = (await Lookups.GetDeliveryMethods()).ToArray();
            var carriers = (await Lookups.GetShippingCarriers()).ToArray();
            if (_disposed)
                return;
            _clients = clients;
            _payments = payments;
            _deliverymen = deliverymen;
            _carriers = carriers;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load order detail lookups.");
            if (!_disposed)
                _error = "تعذر تحميل الاختيارات. بيانات الطلب محفوظة في المحرر؛ حاول مرة أخرى.";
        }
        finally { _loading = false; }
    }

    private Task ChangeAsync<T>(T current, T value, Action<T> apply)
    {
        if (_disposed || EqualityComparer<T>.Default.Equals(current, value))
            return Task.CompletedTask;
        apply(value);
        return DetailsChanged.InvokeAsync();
    }

    private Task SetClientAsync(int? value) => ChangeAsync(State.ClientId, value, v => State.ClientId = v);
    private Task SetPaymentAsync(int? value) => ChangeAsync(State.PaymentMethodId, value, v => State.PaymentMethodId = v);
    private Task SetSourceAsync(OrderSource? value) => ChangeAsync(State.OrderSource, value, v => State.OrderSource = v);
    private Task SetDeliveryAsync(DeliveryMethod? value) => ChangeAsync(State.Delivery.Method, value, v => State.Delivery.Method = v);
    private Task SetDeliverymanAsync(int? value) => ChangeAsync(State.Delivery.DeliverymanId, value, v => State.Delivery.DeliverymanId = v);
    private Task SetCarrierAsync(int? value) => ChangeAsync(State.Delivery.ShippingCarrierId, value, v => State.Delivery.ShippingCarrierId = v);

    private Task SetStepMethodAsync(DeliveryStepEditorState step, DeliveryMethod value)
    {
        if (!State.Delivery.Steps.Contains(step) || value is not (DeliveryMethod.DeliveryMan or DeliveryMethod.ShippingCompany))
            return Task.CompletedTask;
        return ChangeAsync(step.Method, value, v => { step.Method = v; step.HandlerId = null; });
    }

    private Task SetStepHandlerAsync(DeliveryStepEditorState step, int? value) =>
        State.Delivery.Steps.Contains(step) ? ChangeAsync(step.HandlerId, value, v => step.HandlerId = v) : Task.CompletedTask;

    private Task AddStepAsync()
    {
        if (_disposed)
            return Task.CompletedTask;
        State.Delivery.Steps.Add(new() { Method = DeliveryMethod.DeliveryMan });
        return DetailsChanged.InvokeAsync();
    }

    private Task RemoveStepAsync(DeliveryStepEditorState step) =>
        !_disposed && State.Delivery.Steps.Remove(step) ? DetailsChanged.InvokeAsync() : Task.CompletedTask;

    private Task MoveStepAsync(DeliveryStepEditorState step, int offset)
    {
        var index = State.Delivery.Steps.IndexOf(step);
        var target = index + offset;
        if (_disposed || index < 0 || target < 0 || target >= State.Delivery.Steps.Count || offset == 0)
            return Task.CompletedTask;
        State.Delivery.Steps.RemoveAt(index);
        State.Delivery.Steps.Insert(target, step);
        return DetailsChanged.InvokeAsync();
    }

    public void Dispose() => _disposed = true;
}
