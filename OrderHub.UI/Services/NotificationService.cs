using Notification.Wpf;

namespace OrderHub.UI.Services
{
    internal class NotificationService
    {
        private readonly INotificationManager _notificationManager;

        public static NotificationService Instance { get; private set; }
        public static void Initialize(INotificationManager notificationManager)
        {
            Instance ??= new NotificationService(notificationManager);
        }

        private NotificationService(INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
        }

        public void Show(string message)
        {
            _notificationManager.Show(message, NotificationType.Information);
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
