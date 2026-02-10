using Notification.Wpf;
using OrderHub.Application.Interfaces.Services;

namespace OrderHub.UI.Services
{
    internal class NotificationService : INotificationService
    {
        private readonly INotificationManager _notificationManager;

        public NotificationService(INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
        }

        public void ShowError(string message)
        {
            _notificationManager.Show(message, NotificationType.Error);
        }

        public void ShowSuccess(string message)
        {
            _notificationManager.Show(message, NotificationType.Success);
        }
    }
}
