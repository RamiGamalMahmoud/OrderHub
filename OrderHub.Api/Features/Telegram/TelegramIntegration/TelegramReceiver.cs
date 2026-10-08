using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace OrderHub.Api.Features.TelegramIntegration;

public sealed class TelegramReceiver : IHostedService, IDisposable
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUpdateHandler _updateHandler;

    private CancellationTokenSource _receivingCts;

    public TelegramReceiver(
        ITelegramBotClient botClient,
        IUpdateHandler updateHandler)
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _receivingCts = new CancellationTokenSource();

        _botClient.StartReceiving(
            updateHandler: _updateHandler,
            cancellationToken: _receivingCts.Token);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _receivingCts?.Cancel();

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _receivingCts?.Dispose();
    }
}