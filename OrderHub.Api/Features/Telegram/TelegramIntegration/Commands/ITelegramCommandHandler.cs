using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace OrderHub.Api.Features.Telegram.TelegramIntegration.Commands;

public interface ITelegramCommandHandler
{
    string Command { get; }

    bool RequiresAuthentication { get; }

    Task HandleAsync(
        TelegramUpdateContext context,
        string arguments,
        CancellationToken cancellationToken = default);
}
