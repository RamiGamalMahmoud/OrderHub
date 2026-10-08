using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace OrderHub.Api.Features.Telegram.TelegramIntegration.Routing;

public interface IUpdateRouteHandler
{
    bool CanHandle(Update update);

    long? GetTelegramUserId(Update update);

    Task HandleAsync(
        TelegramUpdateContext context,
        CancellationToken cancellationToken = default);
}