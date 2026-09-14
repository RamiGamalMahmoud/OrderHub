using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.UI.Services;

namespace OrderHub.UI.Commands;

public partial class OrdersCommands : ObservableObject, ICommandGroup
{
    [RelayCommand]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Orders.Index.View>();
    }
}
