using System.Text.Json.Serialization;

namespace OrderHub.Api.Features.Telegram.Models;

public class TelegramUpdate
{
    [JsonPropertyName("update_id")]
    public long UpdateId { get; set; }

    [JsonPropertyName("message")]
    public TelegramMessage Message { get; set; }
}
