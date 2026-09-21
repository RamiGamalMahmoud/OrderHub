using OrderHub.Api.Features.Telegram.Models;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Api.Features.Telegram.Services;

public interface ITelegramHttpClient
{
    Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(
        long? offset,
        CancellationToken cancellationToken = default);

    Task SendMessageAsync(
    long chatId,
    string text,
    CancellationToken cancellationToken = default);
}

public sealed class TelegramSendMessageRequest
{
    [JsonPropertyName("chat_id")]
    public long ChatId { get; init; }

    [JsonPropertyName("text")]
    public string Text { get; init; } = string.Empty;
}