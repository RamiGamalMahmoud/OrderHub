using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrderHub.Application.Features.Orders.NotifyOrderParticipants;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Messaging;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class OrdersCommands
    : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IRequestExecutor _requestExecutor;

    public OrdersCommands(
        IAuthorizationService authorization,
        IRequestExecutor requestExecutor)
        : base(authorization)
    {
        _requestExecutor = requestExecutor;

        RegisterAuthorizableCommands(
            OpenCommand,
            CreateCommand,
            EditCommand,
            DeleteCommand,
            BroadcastOrderCommand,
            BroadcastClientCommand,
            BroadcastSupplierCommand,
            BroadcastShippingCarrierCommand,
            BroadcastDeliverymanCommand);
    }

    private bool CanRead =>
        Authorization.CanRead(PermissionFeature.Orders);

    private bool CanWriteOrders => false;

    private bool CanWrite =>
        Authorization.CanWrite(PermissionFeature.Orders);

    // Navigation

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance.NavigateTo<Features.Orders.Index.View>();
    }

    // CRUD

    [RelayCommand(CanExecute = nameof(CanWriteOrders))]
    private async Task Create()
    {
        await DialogService.Instance.ShowDialog<Features.Orders.Create.View>("����� ��� ����");
    }

    [RelayCommand(CanExecute = nameof(CanWriteOrders))]
    private async Task Edit(int id)
    {
        await DialogService.Instance.ShowDialog<Features.Orders.Edit.View>("����� ���", id);
    }

    [RelayCommand(CanExecute = nameof(CanWriteOrders))]
    private Task Delete(int id)
    {
        NotificationService.Instance.ShowError("Use OrderHub Web to archive orders; permanent deletion is unavailable.");
        return Task.CompletedTask;
    }

    // Messaging

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task BroadcastOrder(int id)
    {
        Result result = await _requestExecutor.ExecuteAsync(
            new NotifyOrderParticipantsCommand(
                id,
                null));

        if (result.IsSuccess)
        {
            WeakReferenceMessenger.Default.Send(
                new MessagesMessages.OutboxMessagesQueuedMessage(
                    id));
        }
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private Task BroadcastClient(int id) =>
        BroadcastOrderAsync(
            id,
            RecipientType.Client);

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private Task BroadcastSupplier(int id) =>
        BroadcastOrderAsync(
            id,
            RecipientType.Supplier);

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private Task BroadcastShippingCarrier(int id) =>
        BroadcastOrderAsync(
            id,
            RecipientType.ShippingCarrier);

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private Task BroadcastDeliveryman(int id) =>
        BroadcastOrderAsync(
            id,
            RecipientType.Deliveryman);

    private async Task BroadcastOrderAsync(
        int id,
        RecipientType recipientType)
    {
        Result result = await _requestExecutor.ExecuteAsync(
            new NotifyOrderParticipantsCommand(
                id,
                new NotificationRecipient(recipientType)));

        if (result.IsSuccess)
        {
            WeakReferenceMessenger.Default.Send(
                new MessagesMessages.OutboxMessagesQueuedMessage(
                    id));

            NotificationService.Instance.Show(
                "�� ����� ������� �������, ���� ��� ��� ������� �������� �� ������� ��� �������");

            return;
        }

        NotificationService.Instance
            .ShowError(result.ErrorMessage);
    }
}