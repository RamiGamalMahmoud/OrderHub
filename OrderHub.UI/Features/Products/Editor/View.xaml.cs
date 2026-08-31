using System.Windows;
using System.Windows.Controls;

namespace OrderHub.UI.Features.Products.Editor;

public partial class View : UserControl
{
    public View(ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += View_Loaded;
    }

    private void View_Loaded(object sender, RoutedEventArgs e)
    {
        if(DataContext is ViewModel viewModel)
        {
            Dispatcher.Invoke(viewModel.LoadDataAsync);
        }
    }
}
