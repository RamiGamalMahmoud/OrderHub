using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.UI.Messaging;
using OrderHub.UI.Services;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;

public partial class MessagesCommands : AuthorizableCommandGroup, ICommandGroup
{
    private readonly IRequestExecutor _requestExecutor;

    public MessagesCommands(IAuthorizationService authorization, IRequestExecutor requestExecutor)
        : base(authorization)
    {
        _requestExecutor = requestExecutor;
        RegisterAuthorizableCommands(OpenCommand, DeleteCommand);
    }

    private bool CanRead => Authorization.CanRead(PermissionFeature.Messages);

    [RelayCommand(CanExecute = nameof(CanRead))]
    private void Open()
    {
        NavigationService.Instance
            .NavigateTo<Features.Messages.Index.View>();
    }
    private bool CanDelete(int? id) =>
        id.HasValue && Authorization.CanWrite(PermissionFeature.Messages);

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task Delete(int? id)
    {
        if (!id.HasValue)
            return;

        if (!await DialogService.Instance.ConfirmAsync("هل أنت متأكد أنك تريد حذف هذه الرسالة؟"))
            return;

        var message = (await _requestExecutor.ExecuteAsync(new Application.Queries.OutboxMessageQueries.GetOutboxMessagesQuery()))
            .FirstOrDefault(message => message.Id == id.Value);
        if (message is null)
            return;

        if (!await DialogService.Instance.ConfirmAsync($"هل تريد حذف الرسالة للمستلم ({message.RecipientName})؟"))
            return;

        Result result = await _requestExecutor.ExecuteAsync(new Application.Features.Messaging.DeleteOutboxMessage.Command(id.Value));
        if (!result.IsSuccess)
        {
            NotificationService.Instance.Show(result.ErrorMessage);
            return;
        }

        NotificationService.Instance.ShowSuccess("تم حذف الرسالة بنجاح.");
        WeakReferenceMessenger.Default.Send(new MessagesMessages.OutboxMessageDeletedMessage(id.Value));
    }
}
