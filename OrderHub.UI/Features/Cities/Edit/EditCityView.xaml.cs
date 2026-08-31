using System.Windows.Controls;

namespace OrderHub.UI.Features.Cities.Edit;

public partial class EditCityView : UserControl
{
    public EditCityView(ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadAsync();
    }
}
