using System.Windows.Controls;

namespace OrderHub.UI.Features.Orders.Editor.Components;
public partial class CategoriesView : UserControl
{
    public CategoriesView(object viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
