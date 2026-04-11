using OrderHub.UI.Interfaces;
using System.Windows;

namespace OrderHub.UI.Features.Cities.Edit;

public partial class EditCityView : Window, IDialog
{
    public EditCityView(ViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadAsync();
        viewModel.RequestClose += () => Close();
    }

    public new void Show() => ShowDialog();
}
