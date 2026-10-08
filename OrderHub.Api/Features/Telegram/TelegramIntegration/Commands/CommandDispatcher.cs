using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace OrderHub.Api.Features.Telegram.TelegramIntegration.Commands;

public sealed class CommandDispatcher
{
    private readonly IEnumerable<ITelegramCommandHandler> _commandHandlers;
    private readonly ITelegramBotClient _botClient;

    public CommandDispatcher(IEnumerable<ITelegramCommandHandler> commandHandlers, ITelegramBotClient botClient)
    {
        _commandHandlers = commandHandlers;
        _botClient = botClient;
    }

    public async Task DispatchAsync(TelegramUpdateContext context, CancellationToken cancellationToken = default)
    {
        Message message = context.Update.Message;

        string text = message.Text;

        if (string.IsNullOrWhiteSpace(text))
            return;

        string[] parts = text.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

        string command = parts[0].TrimStart('/');

        int mentionIndex = command.IndexOf('@');

        if (mentionIndex >= 0)
            command = command[..mentionIndex];

        string arguments = parts.Length > 1
            ? parts[1]
            : string.Empty;

        ITelegramCommandHandler handler =
            _commandHandlers.FirstOrDefault(x =>
                string.Equals(
                    x.Command,
                    command,
                    StringComparison.OrdinalIgnoreCase));

        if (handler is null)
            return;

        if(handler.RequiresAuthentication && !context.IsAuthenticated)
        {
            await _botClient.SendMessage(
                message.Chat.Id,
                "You are not authenticated.",
                cancellationToken: cancellationToken);
            return;
        }

        await handler.HandleAsync(context, arguments, cancellationToken);
    }
}
