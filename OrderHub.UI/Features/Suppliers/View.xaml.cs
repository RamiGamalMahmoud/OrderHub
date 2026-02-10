using System.Windows.Controls;

namespace OrderHub.UI.Features.Suppliers;

internal partial class View : UserControl
{
    public View(SuppliersViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += View_Loaded;
    }

    private async void View_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is SuppliersViewModel viewModel)
        {
            await Dispatcher.Invoke(() => viewModel.LoadCommand.ExecuteAsync(null));
        }
    }
}
