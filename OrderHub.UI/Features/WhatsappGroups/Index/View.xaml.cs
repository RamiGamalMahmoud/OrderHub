using System.Windows.Controls;

namespace OrderHub.UI.Features.WhatsappGroups.Index;

internal partial class View : UserControl
{
    public View(WhatsappGroupsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += View_Loaded;
    }

    private async void View_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is WhatsappGroupsViewModel viewModel)
        {
            await Dispatcher.Invoke(() => viewModel.LoadCommand.ExecuteAsync(null));
        }
    }
}
