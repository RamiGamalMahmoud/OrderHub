using System.Windows.Controls;

namespace OrderHub.UI.Features.Settings.Properties;

public partial class PropertiesView : UserControl
{
    private readonly PropertiesViewModel _viewModel;

    public PropertiesView(PropertiesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        _viewModel = viewModel;

        Loaded += PropertiesView_Loaded;
    }

    private void PropertiesView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Dispatcher.Invoke(() => _viewModel.LoadAsync());
    }
}
