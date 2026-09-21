using System.Text.Json.Serialization;

namespace OrderHub.Api.Features.Telegram.Models;

public class TelegramMessage
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }

    [JsonPropertyName("from")]
    public TelegramUser From { get; set; }

    [JsonPropertyName("chat")]
    public TelegramChat Chat { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }
}
