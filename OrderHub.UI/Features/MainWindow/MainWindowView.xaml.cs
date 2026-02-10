using System.Windows;

namespace OrderHub.UI.Features.MainWindow;

internal partial class MainWindowView : Window
{
    public MainWindowView(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
