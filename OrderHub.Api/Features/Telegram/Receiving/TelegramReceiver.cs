using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderHub.Api.Features.Telegram.Models;
using OrderHub.Api.Features.Telegram.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Api.Features.Telegram.Receiving;

public class TelegramReceiver : BackgroundService
{
    private readonly ITelegramHttpClient _client;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public TelegramReceiver(
        ITelegramHttpClient client,
        IServiceScopeFactory serviceScopeFactory)
    {
        _client = client;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        long? offset = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                IReadOnlyList<TelegramUpdate> updates = await _client.GetUpdatesAsync(offset, stoppingToken);

                foreach (var update in updates)
                {
                    await DispatchAsync(update, stoppingToken);
                    offset = update.UpdateId + 1;
                    Debug.WriteLine($"Update id is : {update.UpdateId}");
                    Debug.WriteLine($"Chat id is: {update.Message?.Chat?.Id}");
                }

            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task DispatchAsync(
        TelegramUpdate update,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetRequiredService<IEnumerable<ITelegramUpdateHandler>>();
        var handler = handlers.FirstOrDefault(x => x.CanHandle(update));
        if (handler is null)
            return;
        await handler.HandleAsync(update, cancellationToken);
    }
}
