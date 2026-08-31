using System.Windows;

namespace OrderHub.UI.Features.ShippingCarriers.Editor;

public partial class View : Window
{
    public View(ViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        Loaded += async (_, _) => await Dispatcher.Invoke(viewModel.LoadAsync);
        viewModel.RequestClose += () => Close();
    }

    public new void Show() => ShowDialog();
}
