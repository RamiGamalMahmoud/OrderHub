using Telegram.Bot;

namespace OrderHub.Api.Features.Telegram.TelegramIntegration.Commands;

public abstract class CommandHandlerBase
{
    protected readonly ITelegramBotClient _botClient;

    protected CommandHandlerBase(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }
}
