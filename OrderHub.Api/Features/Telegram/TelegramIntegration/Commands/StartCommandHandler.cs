using MediatR;
using OrderHub.Api.Features.TelegramIntegration.Linking.Complete;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace OrderHub.Api.Features.Telegram.TelegramIntegration.Commands;

public sealed class StartCommandHandler : CommandHandlerBase, ITelegramCommandHandler
{
    private readonly IMediator _mediator;

    public StartCommandHandler(
        IMediator mediator,
        ITelegramBotClient botClient) : base(botClient)
    {
        _mediator = mediator;
    }

    public string Command => "start";

    public bool RequiresAuthentication => false;
    public async Task HandleAsync(
        TelegramUpdateContext context,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        Message message = context.Update.Message;
        if (string.IsNullOrWhiteSpace(arguments))
        {
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "⚠️ رابط الربط غير صالح.",
                cancellationToken: cancellationToken);

            return;
        }

        if (message.From is null)
            return;

        CompleteTelegramLinkingCommand command = new(
            arguments,
            message.From.Id,
            message.Chat.Id,
            message.From.Username,
            message.From.FirstName,
            message.From.LastName);

        var result = await _mediator.Send(
            command,
            cancellationToken);

        if (!result.IsSuccess)
        {
            await _botClient.SendMessage(
                chatId: message.Chat.Id,
                text: result.ErrorMessage,
                cancellationToken: cancellationToken);

            return;
        }

        TelegramAccount account = result.Value;

        BotCommand[] commands = account.OwnerType switch
        {
            TelegramAccountOwnerType.User =>
            [
                new("orders", "عرض الطلبات"),
        new("help", "المساعدة")
            ],

            TelegramAccountOwnerType.Deliveryman =>
            [
                new("myorders", "طلباتي"),
        new("help", "المساعدة")
            ],

            TelegramAccountOwnerType.Supplier =>
            [
                new("orders", "الطلبات"),
        new("help", "المساعدة")
            ],

            TelegramAccountOwnerType.ShippingCarrier =>
            [
                new("shipments", "الشحنات"),
        new("help", "المساعدة")
            ],

            _ => []
        };

        await _botClient.SetMyCommands(
            commands,
            scope: new BotCommandScopeChat() { ChatId = account.ChatId},
            cancellationToken: cancellationToken);

        await _botClient.SetMyCommands(commands, cancellationToken: cancellationToken);

        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "✅ تم ربط حسابك بـ OrderHub بنجاح.",
            cancellationToken: cancellationToken);
    }
}
