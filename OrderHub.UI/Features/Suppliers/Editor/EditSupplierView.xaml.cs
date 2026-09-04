using System.Windows;
using System.Windows.Controls;

namespace OrderHub.UI.Features.Suppliers.Editor;

internal partial class EditSupplierView : UserControl
{
    public EditSupplierView(EditSupplierViewModelBase viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += CreateView_Loaded;
    }

    private async void CreateView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is EditSupplierViewModelBase viewModdel)
        {
            await Dispatcher.Invoke(async () => await viewModdel.LoadAsync());
        }
    }
}
