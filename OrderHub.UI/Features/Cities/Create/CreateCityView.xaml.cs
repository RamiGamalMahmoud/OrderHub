using OrderHub.UI.Interfaces;
using System.Windows;

namespace OrderHub.UI.Features.Cities.Create;

public partial class CreateCityView : Window, IDialog
{
    public CreateCityView(CreateCityViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseEditorRequest += () => Close();
    }

    public new void Show() => ShowDialog();

    private void Button_Click(object sender, RoutedEventArgs e) => Close();
}
