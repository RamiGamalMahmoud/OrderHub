using CommunityToolkit.Mvvm.Input;
using OrderHub.Application.Features.Telegram.Links.Create;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Threading.Tasks;

namespace OrderHub.UI.Commands;
public partial class TelegramCommands : AuthorizableCommandGroup, ICommandGroup
{
    private IRequestExecutor _requestExecutor;
    public TelegramCommands(
        IAuthorizationService authorization,
        IRequestExecutor requestExecutor)
        : base(authorization)
    {
        _requestExecutor = requestExecutor;
    }

    [RelayCommand]
    private async Task<Result<string>> CreateLink(CreateTelegramLinkCommandRequest request)
    {
        CreateTelegramLinkRequest createTelegramLinkRequest = new CreateTelegramLinkRequest(
            request.ownerType,
            request.ownerId);

        Result<CreateTelegramLinkResult> result = await _requestExecutor.ExecuteAsync(new CreateTelegramLinkCommand(createTelegramLinkRequest));

        if (result.IsSuccess)
            return Result<string>.Success(result.Value.Link);

        return Result<string>.Failure(result.ErrorMessage);
    }
}

public record CreateTelegramLinkCommandRequest(TelegramAccountOwnerType ownerType, int ownerId);
