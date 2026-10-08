using OrderHub.Api.Features.Telegram.TelegramIntegration.Commands;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OrderHub.Api.Features.Telegram.TelegramIntegration.Routing;

public class CommandUpdateHandler : IUpdateRouteHandler
{
    private readonly CommandDispatcher _commandDispatcher;

    public CommandUpdateHandler(CommandDispatcher commandDispatcher)
    {
        _commandDispatcher = commandDispatcher;
    }

    public bool CanHandle(Update update)
    {
        return update.Type == UpdateType.Message
            && update.Message?.Text?.StartsWith('/') == true;
    }
    public long? GetTelegramUserId(Update update) => update.Message?.From?.Id;

    public Task HandleAsync(TelegramUpdateContext context, CancellationToken cancellationToken = default)
    {
        return _commandDispatcher.DispatchAsync(context, cancellationToken);
    }
}
