using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.UI.Services;

namespace OrderHub.UI.Commands;

public partial class SuppliersCommands : ObservableObject, ICommandGroup
{
    [RelayCommand]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Suppliers.Index.View>();
    }
}
