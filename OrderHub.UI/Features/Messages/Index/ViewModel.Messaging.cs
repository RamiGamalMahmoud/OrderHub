using CommunityToolkit.Mvvm.Messaging;
using OrderHub.UI.Messaging;

namespace OrderHub.UI.Features.Messages.Index;

public partial class ViewModel
{
    private void RegisterMessages()
    {
        WeakReferenceMessenger.Default.Register<
            Application.Messages.OutboxMessages.MessageStatusChangedMessage>(
            this,
            async (_, _) =>
            {
                await ReloadAsync();
            });

        WeakReferenceMessenger.Default.Register<
            MessagesMessages.OutboxMessagesQueuedMessage>(
            this,
            async (_, _) =>
            {
                await ReloadAsync();
            });

        WeakReferenceMessenger.Default.Register<
            MessagesMessages.OutboxMessageDeletedMessage>(
            this,
            async (_, message) =>
            {
                if (SelectedMessage?.Id == message.MessageId)
                {
                    SelectedMessage = null;
                }

                await ReloadAsync();
            });
    }
}