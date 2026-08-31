using System.Windows;

namespace OrderHub.UI.Features.Settings.ClientCredentialsSettings;

internal partial class ClientCredentialsSettingsView : Window
{
    public ClientCredentialsSettingsView(ClientCredentialsSettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += ClientCredentialsSettingsView_Loaded;
    }

    private void ClientCredentialsSettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ClientCredentialsSettingsViewModel viewModel)
        {
            Dispatcher.InvokeAsync(() =>
            {
                viewModel.LoadCredentialsCommand.Execute(null);
            });
        }
    }

    public new void Show() => ShowDialog();

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
