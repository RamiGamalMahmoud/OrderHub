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

public partial class WhatsappGroupsCommands : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IMediator _mediator;

    public WhatsappGroupsCommands(IAuthorizationService authorization, IMediator mediator)
        : base(authorization)
    {
        _mediator = mediator;
        RegisterAuthorizableCommands(OpenCommand, CreateCommand, EditCommand, DeleteCommand);
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.WhatsAppGroups);

    private bool CanWrite => Authorization.CanWrite(PermissionFeature.WhatsAppGroups);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.WhatsappGroups.Index.View>();
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Create(string title)
    {
        await DialogService.Instance.ShowDialog<Features.WhatsappGroups.Create.View>(title ?? "إضافة مجموعة واتساب جديدة");
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Edit(int id)
    {
        await DialogService.Instance.ShowDialog<Features.WhatsappGroups.Update.View>("تعديل بيانات مجموعة واتساب", id);
    }

    [RelayCommand(CanExecute = nameof(CanWrite))]
    private async Task Delete(int id)
    {
        var item = await _mediator.Send(new Application.Queries.WhatsappGroupQueries.GetAllWhatsappGroupForEditQuery(id));
        if (item is null)
            return;

        if (DialogService.Instance.Confirm($"هل تريد حذف مجموعة الواتساب {item.Name}"))
        {
            Result result = await _mediator.Send(new Application.Commands.WhatsappGroupCommands.DeleteWhatsappGroupCommand(id));
            if(result.IsSuccess)
            {
                NotificationService.Instance.ShowSuccess(MessageBuilder.Build(MessageBuilder.OperationType.Delete, true, "مجموعة الواتساب"));
                WeakReferenceMessenger.Default.Send(new WhatsAppGroupsMessages.WhatsappGroupDeletedMessage(id));
            }
            else
            {
                NotificationService.Instance.ShowError(MessageBuilder.Build(MessageBuilder.OperationType.Delete, false, "مجموعة الواتساب"));
            }
        }
    }
}
