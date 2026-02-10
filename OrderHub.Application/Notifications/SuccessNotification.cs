using MediatR;
using OrderHub.Application.Interfaces.Services;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Notifications;

public record SuccessNotification(string Message) : INotification;

internal class SuccessNotificationHandler(INotificationService notificationService) : INotificationHandler<SuccessNotification>
{
    public Task Handle(SuccessNotification notification, CancellationToken cancellationToken)
    {
        notificationService.ShowSuccess(notification.Message);
        return Task.CompletedTask;
    }
}
