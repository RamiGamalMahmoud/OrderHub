using System.Windows.Controls;

namespace OrderHub.UI.Features.Home;

internal partial class HomeView : UserControl
{
    public HomeView(HomeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
