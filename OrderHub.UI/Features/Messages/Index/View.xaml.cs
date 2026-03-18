using System.Windows;
using System.Windows.Controls;

namespace OrderHub.UI.Features.Messages.Index;

public partial class View : UserControl
{
    private bool _isLoaded = false;
    public View(ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += View_Loaded;
    }

    private async void View_Loaded(object sender, RoutedEventArgs e)
    {
        if(!_isLoaded && DataContext is ViewModel viewModel)
        {
            await viewModel.LoadAsync();
            _isLoaded = true;
        }
    }
}
