using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Messaging;
using OrderHub.UI.Services;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class CategoriesCommands : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IMediator _mediator;
    public CategoriesCommands(
        IAuthorizationService authorization,
        IMediator mediator) : base(authorization)
    {
        RegisterAuthorizableCommands(
            OpenCommand,
            CreateCommand,
            EditCommand,
            DeleteCommand);
        _mediator = mediator;
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.Categories);
    private bool CanWrite => Authorization.CanWrite(PermissionFeature.Categories);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Categories.Index.View>();
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Create()
    {
        await DialogService.Instance.ShowDialog<Features.Categories.Create.View>("إضافة قسم جديد");
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Edit(int id)
    {
        await DialogService.Instance.ShowDialog<Features.Categories.Update.View>("تعديل بيانات قسم", id);
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Delete(int id)
    {
        var category = await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryForEditQuery(id));
        if (category is null)
            return;
        var dto = (await _mediator.Send(new Application.Queries.CategoryQueries.GetCategoryListQuery(category.ParentId)))
            .FirstOrDefault(item => item.Id == id);
        if (dto is null)
            return;

        StringBuilder stringBuilder = new StringBuilder();

        if(dto.SubCategoriesCount > 0)
        {
            stringBuilder.AppendLine($"القسم يحتوي على {dto.SubCategoriesCount} قسم فرعي");
        }

        if(dto.ProductsCount > 0)
        {
            stringBuilder.AppendLine($"القسم يحتوي على {dto.ProductsCount} منتج");
        }
        stringBuilder.AppendLine("سوف يتم حذف القسم الذي تم اختياره؟");
        stringBuilder.AppendLine("و جميع الأقسام التابعة له و كذلك المنتجات المرتبطة به");
        stringBuilder.AppendLine("و كذلك عمليات الشراء");

        if (!DialogService.Instance.Confirm(stringBuilder.ToString()))
            return;
        Result result =await _mediator.Send(new Application.Commands.CategoryCommands.DeleteCategoryCommand(id));
        if(result.IsSuccess)
        {
            WeakReferenceMessenger.Default.Send(new CategoriesMessages.CategoryDeletedMessage(id));
            NotificationService.Instance.ShowError("تم حذف القسم");
        }
        else
        {
            NotificationService.Instance.ShowError(result.ErrorMessage);
        }
    }
}
