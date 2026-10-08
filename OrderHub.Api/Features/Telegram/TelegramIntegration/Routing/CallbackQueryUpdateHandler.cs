using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OrderHub.Api.Features.Telegram.TelegramIntegration.Routing;

public class CallbackQueryUpdateHandler : IUpdateRouteHandler
{
    private readonly ITelegramBotClient _botClient;

    public CallbackQueryUpdateHandler(ITelegramBotClient botClient)
    {
        _botClient = botClient;
    }

    public bool CanHandle(Update update) => update.Type == UpdateType.CallbackQuery;

    public long? GetTelegramUserId(Update update) => update.CallbackQuery.Message?.From?.Id;

    public async Task HandleAsync(TelegramUpdateContext context, CancellationToken cancellationToken = default)
    {
        CallbackQuery callback = context.Update.CallbackQuery;

        await _botClient.EditMessageText(
            chatId: callback.Message.Chat.Id,
            messageId: callback.Message.MessageId,
            text: callback.Data);
    }
}
