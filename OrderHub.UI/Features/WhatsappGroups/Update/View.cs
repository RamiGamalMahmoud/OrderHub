using System.Windows;

namespace OrderHub.UI.Features.WhatsappGroups.Update;

internal class View : Editor.View
{
    public View(ViewModel viewModel) : base(viewModel)
    {
        Loaded += View_Loaded;
    }

    private void View_Loaded(object sender, RoutedEventArgs e)
    {
        if(DataContext is ViewModel viewModel)
        {
            Dispatcher.InvokeAsync(viewModel.LoadAsync);
        }
    }
}
