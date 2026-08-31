using System.Windows.Controls;

namespace OrderHub.UI.Features.Categories.Editor;

public partial class View : UserControl
{
    public View(ViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        Loaded += (s, e) => Dispatcher.Invoke(() => viewModel.LoadDataAsync());
    }
}
