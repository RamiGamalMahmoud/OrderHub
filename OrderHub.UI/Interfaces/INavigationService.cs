namespace OrderHub.UI.Interfaces
{
    public interface INavigationService
    {
        object CurrentView { get; }

        TView NavigateTo<TView>() where TView : class;
    }
}
