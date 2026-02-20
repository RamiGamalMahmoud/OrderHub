using OrderHub.UI.Interfaces;
using System.Windows;

namespace OrderHub.UI.Features.Orders.Editor;

internal partial class View : Window, IDialog
{
    public View(ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += async (_, _) => await viewModel.LoadAsync();
        viewModel.RequestClose += () => Close();
    }
}
