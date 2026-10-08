using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.OrderDrafts.Delete;
using OrderHub.Application.Features.OrderDrafts.List;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.UI.Messaging;
using System;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class OrderDraftsCommands : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IRequestExecutor _requestExecutor;

    public OrderDraftsCommands(IAuthorizationService authorization, IRequestExecutor requestExecutor)
        : base(authorization)
    {
        _requestExecutor = requestExecutor;
        RegisterAuthorizableCommands(DeleteCommand);
    }

    private bool CanDelete(Guid? id) =>
        id.HasValue && Authorization.CanWrite(PermissionFeature.Orders);

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task Delete(Guid? id)
    {
        if (!id.HasValue)
            return;
        if (!DialogService.Instance.Confirm("هل تريد حذف هذه المسودة؟"))
            return;

        var result = await _requestExecutor.ExecuteAsync(new DeleteDraftCommand(id.Value));
        if (!result.IsSuccess) throw new System.InvalidOperationException(result.ErrorMessage);
        WeakReferenceMessenger.Default.Send(new OrdersMessages.OrderDraftDeletedMessage(id.Value));
    }
}
