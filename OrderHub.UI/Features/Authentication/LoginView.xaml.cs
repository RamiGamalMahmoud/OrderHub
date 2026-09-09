using System.Windows;

namespace OrderHub.UI.Features.Authentication;

public partial class LoginView : Window
{
    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
