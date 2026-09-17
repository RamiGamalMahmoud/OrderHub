using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.UI.Messaging;
using System;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class OrderDraftsCommands : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IDraftService _draftService;

    public OrderDraftsCommands(IAuthorizationService authorization, IDraftService draftService)
        : base(authorization)
    {
        _draftService = draftService;
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

        await _draftService.DeleteAsync(new DraftContext(id.Value));
        WeakReferenceMessenger.Default.Send(new OrdersMessages.OrderDraftDeletedMessage(id.Value));
    }
}
