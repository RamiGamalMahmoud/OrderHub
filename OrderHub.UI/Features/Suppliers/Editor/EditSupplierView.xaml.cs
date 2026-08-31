using System.Windows;

namespace OrderHub.UI.Features.Suppliers.Editor;

internal partial class EditSupplierView : Window
{
    public EditSupplierView(EditSupplierViewModelBase viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += CreateView_Loaded;
        viewModel.RequestClose += () => Close();
    }

    private async void CreateView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is EditSupplierViewModelBase viewModdel)
        {
            await Dispatcher.Invoke(async () => await viewModdel.LoadAsync());
        }
    }

    public new void Show() => ShowDialog();
}
