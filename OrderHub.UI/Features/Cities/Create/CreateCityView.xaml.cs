using System.Windows.Controls;

namespace OrderHub.UI.Features.Cities.Create;

public partial class CreateCityView : UserControl
{
    public CreateCityView(CreateCityViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
