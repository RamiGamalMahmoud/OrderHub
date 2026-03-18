using MediatR;
using OrderHub.Application.Interfaces.Services;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Notifications;

public record AppliationNotification(string Message) : INotification;

internal class ApplicationNotificationHandler(INotificationService notificationService) : INotificationHandler<AppliationNotification>
{
    public Task Handle(AppliationNotification notification, CancellationToken cancellationToken)
    {
        notificationService.Show(notification.Message);
        return Task.CompletedTask;
    }
}
