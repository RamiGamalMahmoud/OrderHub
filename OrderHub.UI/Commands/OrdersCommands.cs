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

    private bool CanWrite =>
        Authorization.CanWrite(PermissionFeature.Orders);

    // Navigation

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance.NavigateTo<Features.Orders.Index.View>();
    }

    // CRUD

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Create()
    {
        await DialogService.Instance.ShowDialog<Features.Orders.Create.View>("≈‰‘«¡ ÿ·» ÃœÌœ");
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Edit(int id)
    {
        await DialogService.Instance.ShowDialog<Features.Orders.Edit.View>(" ⁄œÌ· ÿ·»", id);
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Delete(int id)
    {
        if (!DialogService.Instance.Confirm($"Â·  —Ìœ Õ–› «·ÿ·»ø"))
        {
            return;
        }

        Result result = await _requestExecutor.ExecuteAsync(new Application.Features.Orders.Delete.DeleteOrder.Command(id));

        if (!result.IsSuccess)
        {
            NotificationService.Instance
                .ShowError(result.ErrorMessage);

            return;
        }

        WeakReferenceMessenger.Default.Send(
            new OrdersMessages.OrderDeletedMessage(id));

        NotificationService.Instance
            .ShowSuccess(" „ Õ–› «·ÿ·» »‰Ã«Õ.");
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
                " „ «÷«›… «·—”«·… ··≈—”«·,  ÊÃÂ ≈·Ï ”Ã· «·—”«∆· ·≈—”«·Â« «Ê  Œ’Ì’Â« ﬁ»· «·≈—”«·");

            return;
        }

        NotificationService.Instance
            .ShowError(result.ErrorMessage);
    }
}