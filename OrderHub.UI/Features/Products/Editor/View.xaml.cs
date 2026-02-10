using OrderHub.UI.Interfaces;
using System.Windows;

namespace OrderHub.UI.Features.Products.Editor;

public partial class View : Window, IDialog
{
    public View(ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += View_Loaded;
        viewModel.RequestClose += () => Close();
    }

    private void View_Loaded(object sender, RoutedEventArgs e)
    {
        if(DataContext is ViewModel viewModel)
        {
            Dispatcher.Invoke(viewModel.LoadDataAsync);
        }
    }

    public new void Show() => ShowDialog();
}
