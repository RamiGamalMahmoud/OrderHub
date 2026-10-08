using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MediatR;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Common;
using OrderHub.UI.Messaging;
using OrderHub.UI.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class SuppliersCommands : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IMediator _mediator;

    public SuppliersCommands(IAuthorizationService authorization, IMediator mediator)
        : base(authorization)
    {
        _mediator = mediator;
        RegisterAuthorizableCommands(OpenCommand, CreateCommand, EditCommand, DeleteCommand);
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.Suppliers);

    private bool CanWrite => Authorization.CanWrite(PermissionFeature.Suppliers);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Suppliers.Index.View>();
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Create(string title)
    {
        await DialogService.Instance.ShowDialog<Features.Suppliers.Create.View>(title ?? "إضافة مورد جديد");
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Edit(int id)
    {
        await DialogService.Instance.ShowDialog<Features.Suppliers.Update.View>("تعديل طلب", id);
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Delete(int id)
    {
        var item = await _mediator.Send(new Application.Queries.SupplierQueries.GetSupplierForEditQuery(id));
        if (item is null)
            return;

        if (DialogService.Instance.Confirm($"هل تريد حذف المورد {item.Name}"))
        {
            Result result = await _mediator.Send(new Application.Commands.SupplierCommands.DeleteSupplierCommand(id));
            if (result.IsSuccess)
            {
                NotificationService.Instance.ShowSuccess(MessageBuilder.Build(MessageBuilder.OperationType.Delete, true, "المورد"));
                WeakReferenceMessenger.Default.Send(new SuppliersMessages.SupplierDeletedMessage(id));
            }
            else
            {
                NotificationService.Instance.ShowError(MessageBuilder.Build(MessageBuilder.OperationType.Delete, false, "المورد"));
            }
        }
    }
}
