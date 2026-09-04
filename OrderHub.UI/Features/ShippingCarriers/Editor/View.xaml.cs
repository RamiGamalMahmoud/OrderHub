using System.Windows;
using System.Windows.Controls;

namespace OrderHub.UI.Features.ShippingCarriers.Editor;

public partial class View : UserControl
{
    public View(ViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        Loaded += async (_, _) => await Dispatcher.Invoke(viewModel.LoadAsync);
    }
}
