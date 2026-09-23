using Microsoft.Extensions.Options;
using OrderHub.Api.Features.Telegram.Models;
using OrderHub.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Api.Features.Telegram.Services;

public class TelegramHttpClient : ITelegramHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IConnectionService _connectionService;
    private readonly TelegramOptions _options;

    public TelegramHttpClient(
        HttpClient httpClient,
        IOptions<TelegramOptions> options,
        IConnectionService connectionService)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _connectionService = connectionService;
    }

    public async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(long? offset, CancellationToken cancellationToken = default)
    {
        await _connectionService.CheckConnectionAsync();
        if (!_connectionService.IsConnected)
            return [];

        string url = BuildGetUpdatesUrl(offset);

        using HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync(cancellationToken);

        TelegramUpdatesResponse updatesResponse = JsonSerializer.Deserialize<TelegramUpdatesResponse>(content);

        if (updatesResponse is null)
            throw new InvalidOperationException("Telegram returned an invalid response.");

        if (!updatesResponse.Ok)
            throw new InvalidOperationException("Telegram getUpdates request failed.");

        return updatesResponse.Result;
    }

    public async Task SendMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        TelegramSendMessageRequest request = new TelegramSendMessageRequest()
        {
            ChatId = chatId,
            Text = text
        };
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
            "sendMessage", 
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private string BuildGetUpdatesUrl(long? offset)
    {
        string url =
            $"https://api.telegram.org/bot{_options.BotToken}/getUpdates?timeout=30";

        if (offset.HasValue)
            url += $"&offset={offset.Value}";

        return url;
    }
}
