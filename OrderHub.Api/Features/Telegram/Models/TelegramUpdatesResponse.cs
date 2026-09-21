using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OrderHub.Api.Features.Telegram.Models;

public class TelegramUpdatesResponse
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    [JsonPropertyName("result")]
    public List<TelegramUpdate> Result { get; set; }
}
