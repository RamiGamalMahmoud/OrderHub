using Microsoft.Extensions.Hosting;
using OrderHub.Application.Interfaces.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.UI.Workers;
internal class ConnectionMonitorWorker : BackgroundService
{
    private readonly IConnectionService _connectionService;

    public ConnectionMonitorWorker(IConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            await _connectionService.CheckConnectionAsync(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
