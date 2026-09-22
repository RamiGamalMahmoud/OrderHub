using MediatR;
using OrderHub.Api.Features.Telegram.Links.Complete;
using OrderHub.Api.Features.Telegram.Models;
using OrderHub.Api.Features.Telegram.Services;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Api.Features.Telegram.Receiving.Handlers;

public class TelegramStartUpdateHandler : ITelegramUpdateHandler
{
    private readonly ITelegramHttpClient _httpClient;
    private readonly IMediator _mediator;

    public TelegramStartUpdateHandler(
        ITelegramHttpClient httpClient,
        IMediator mediator)
    {
        _httpClient = httpClient;
        _mediator = mediator;
    }

    public bool CanHandle(TelegramUpdate update)
    {
        return update.Message is not null &&
            !string.IsNullOrWhiteSpace(update.Message.Text) &&
            update.Message.Text.StartsWith("/start ", System.StringComparison.Ordinal);
    }

    public async Task HandleAsync(TelegramUpdate update, CancellationToken cancellationToken = default)
    {
        string token = update.Message.Text["/start".Length..].Trim();
        CompleteTelegramLinkingCommand command = new CompleteTelegramLinkingCommand(
            token,
            update.Message.From.Id,
            update.Message.Chat.Id,
            update.Message.From.Username,
            update.Message.From.FirstName,
            update.Message.From.LastName);

        var result = await _mediator.Send(command, cancellationToken);
        string responseMessage = result.IsSuccess
            ? "تم ربط حسابك في Telegram بـ OrderHub بنجاح"
            : result.ErrorMessage;

        await _httpClient.SendMessageAsync(
            update.Message.Chat.Id,
            responseMessage,
            cancellationToken);
    }
}
