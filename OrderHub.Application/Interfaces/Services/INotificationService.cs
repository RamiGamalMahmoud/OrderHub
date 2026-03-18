namespace OrderHub.Application.Interfaces.Services
{
    public interface INotificationService
    {
        void ShowError(string message);
        void ShowSuccess(string message);
        void Show(string message);
    }
}
