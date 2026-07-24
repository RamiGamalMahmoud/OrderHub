using System.Windows.Controls;

namespace OrderHub.UI.Features.Settings.Home;

public partial class SettingsHomeView : UserControl
{
    public SettingsHomeView(SettingsHomeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
