using CommunityToolkit.Mvvm.Messaging;
using OrderHub.Domain.Enums;
using OrderHub.UI.Messaging;
using System.Linq;

namespace OrderHub.UI.Features.Orders.Index;
internal partial class ViewModel
{
    private void RegisterMessages()
    {
        WeakReferenceMessenger.Default
            .Register<OrdersMessages.OrderCreatedMessage>(
                this,
                async (_, _) => await ReloadAsync());

        WeakReferenceMessenger.Default
            .Register<OrdersMessages.OrderUpdatedMessage>(
                this,
                async (_, _) => await ReloadAsync());

        WeakReferenceMessenger.Default
            .Register<OrdersMessages.OrderStatusChangedMessage>(
                this,
                async (_, _) => await ReloadAsync());

        WeakReferenceMessenger.Default
            .Register<OrdersMessages.OrderPaymentMethodChangedMessage>(
                this,
                async (_, _) => await ReloadAsync());

        WeakReferenceMessenger.Default
            .Register<OrdersMessages.OrderDeletedMessage>(
                this,
                async (_, _) => await ReloadAsync());

        WeakReferenceMessenger.Default.Register<
            Application.Messages.OutboxMessages.MessageStatusChangedMessage>(
            this,
            (_, message) =>
            {
                OrderViewModel order =
                    Orders.FirstOrDefault(
                        item => item.Id == message.OrderId);

                order?.UpdateRecipientStatus(
                    message.RecipientType,
                    message.NewStatus == OutboxMessageStatus.Sent);
            });

        WeakReferenceMessenger.Default.Register<
            ViewModel,
            OrdersMessages.OrderDraftSavedMessage>(
            this,
            (_, _) =>
            {
                OnPropertyChanged(
                    nameof(OrderDraftsDrawerViewModel.Drafts.Count));
            });

        WeakReferenceMessenger.Default.Register<
            ViewModel,
            OrdersMessages.OrderDraftDeletedMessage>(
            this,
            (_, _) =>
            {
                OnPropertyChanged(
                    nameof(OrderDraftsDrawerViewModel.Drafts.Count));
            });
    }
}
