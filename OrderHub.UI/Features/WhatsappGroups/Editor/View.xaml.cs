using OrderHub.UI.Interfaces;
using System.Windows;

namespace OrderHub.UI.Features.WhatsappGroups.Editor;

public partial class View : Window, IDialog
{
    public View(ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public new void Show() => ShowDialog();
}
