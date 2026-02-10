using OrderHub.UI.Interfaces;
using System.Windows;

namespace OrderHub.UI.Features.Categories.Editor;

public partial class View : Window, IDialog
{
    public View(ViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        Loaded += (s, e) => Dispatcher.Invoke(() => viewModel.LoadDataAsync());
        viewModel.RequestClose += () => Close();
    }

    private void Button_Click(object sender, RoutedEventArgs e) => Close();
}
