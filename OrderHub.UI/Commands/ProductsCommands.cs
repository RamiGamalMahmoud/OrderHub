using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrderHub.UI.Services;

namespace OrderHub.UI.Commands;

public partial class ProductsCommands : ObservableObject, ICommandGroup
{
    [RelayCommand]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Products.Index.View>();
    }
}
