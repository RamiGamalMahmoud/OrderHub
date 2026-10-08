using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Messaging;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class ProductsCommands : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IMediator _mediator;

    public ProductsCommands(IAuthorizationService authorization, IMediator mediator)
        : base(authorization)
    {
        _mediator = mediator;
        RegisterAuthorizableCommands(OpenCommand, CreateCommand, EditCommand, DeleteCommand);
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.Products);

    private bool CanWrite => Authorization.CanWrite(PermissionFeature.Products);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Products.Index.View>();
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Create(string title)
    {
        await DialogService.Instance.ShowDialog<Features.Products.Create.View>(title ?? "إضافة منتج جديد");
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Edit(int id)
    {
        await DialogService.Instance.ShowDialog<Features.Products.Update.View>("تعديل بيانات منتج", id);
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Delete(int id)
    {
        var item = await _mediator.Send(new Application.Features.Products.Get.GetProduct.Query(id));
        if (item is null)
            return;

        if (!DialogService.Instance.Confirm($"هل تريد حذف المنتج( {item.Name})"))
        {
            return;
        }

        Result result = await _mediator.Send(new Application.Features.Products.Delete.DeleteProductCommand(id));
        if (result.IsSuccess)
        {
            NotificationService.Instance.ShowSuccess("تم حذف المنتج");
            WeakReferenceMessenger.Default.Send(new ProductsMessages.ProductDeletedMessage(id));
        }
        else
        {
            NotificationService.Instance.ShowError("خطأ أثناء حذف المنتج");
        }
    }
}
