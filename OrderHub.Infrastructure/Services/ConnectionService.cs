using OrderHub.Application.Interfaces.Services;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal sealed class ConnectionService : IConnectionService
{
    private const string HOST = "8.8.8.8";

    public bool IsConnected { get; private set; }

    public event EventHandler<bool> ConnectionChanged;

    public async Task CheckConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        bool isConnected = await CheckAsync(cancellationToken);

        if (isConnected == IsConnected)
            return;

        IsConnected = isConnected;

        ConnectionChanged?.Invoke(this, isConnected);
    }

    private static async Task<bool> CheckAsync(
        CancellationToken cancellationToken)
    {
        using Ping ping = new();

        try
        {
            PingReply reply = await ping.SendPingAsync(HOST);

            return reply.Status == IPStatus.Success;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }
}