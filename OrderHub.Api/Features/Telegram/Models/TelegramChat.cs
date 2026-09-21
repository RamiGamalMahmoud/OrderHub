using System.Text.Json.Serialization;

namespace OrderHub.Api.Features.Telegram.Models;

public class TelegramChat
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }
}