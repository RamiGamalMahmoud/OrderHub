namespace OrderHub.UI.Interfaces
{
    public interface IDialogService
    {
        bool Confirm(string message);
        void ShowDialog<TView>(object parameter = null) where TView : IDialog;
    }
}
