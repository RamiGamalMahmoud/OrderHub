using MediatR;
using OrderHub.Application.Interfaces.Services;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Notifications;

public record ErrorNotification(string Message) : INotification;

internal class ErrorNotificationHandler(INotificationService notificationService) : INotificationHandler<ErrorNotification>
{
    public Task Handle(ErrorNotification notification, CancellationToken cancellationToken)
    {
        notificationService.ShowError(notification.Message);
        return Task.CompletedTask;
    }
}
