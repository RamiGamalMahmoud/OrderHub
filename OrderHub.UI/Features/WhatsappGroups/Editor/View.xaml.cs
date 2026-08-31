using System.Windows.Controls;

namespace OrderHub.UI.Features.WhatsappGroups.Editor;

public partial class View : UserControl
{
    public View(ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
