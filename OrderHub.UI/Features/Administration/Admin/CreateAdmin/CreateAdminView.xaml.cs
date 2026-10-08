using System.Windows;

namespace OrderHub.UI.Features.Administration.Admin.CreateAdmin;

public partial class CreateAdminView : Window
{
    public CreateAdminView(CreateAdminViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
