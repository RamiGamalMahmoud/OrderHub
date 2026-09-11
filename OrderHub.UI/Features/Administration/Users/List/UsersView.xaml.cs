using System.Windows;
using System.Windows.Controls;

namespace OrderHub.UI.Features.Administration.Users.List;

public partial class UsersView : UserControl
{
    public UsersView(UsersViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
        Loaded += UsersView_Loaded;
    }

    private async void UsersView_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not UsersViewModel usersViewModel)
            return;

        await usersViewModel.LoadAsync();

        Loaded -= UsersView_Loaded;
    }
}