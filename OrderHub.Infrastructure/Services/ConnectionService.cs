using OrderHub.Application.Interfaces.Services;
using System;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class ConnectionService : IConnectionService
{
    private readonly SemaphoreSlim _stateLock = new(1, 1);
    private readonly Ping _ping = new();
    private readonly SynchronizationContext _context = SynchronizationContext.Current;

    private CancellationTokenSource _cts;
    private Task _worker;

    public bool IsConnected { get; private set; }

    public event EventHandler<bool> ConnectionChanged;

    public async Task Start()
    {
        await _stateLock.WaitAsync();

        try
        {
            if (_worker is { IsCompleted: false })
                return;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _worker = Task.Run(() => MonitorConnection(_cts.Token));
        }
        finally
        {
            _stateLock.Release();
        }
    }

    public async Task Stop()
    {
        await _stateLock.WaitAsync();

        try
        {
            _cts?.Cancel();

            if (_worker != null)
            {
                try
                {
                    await _worker;
                }
                catch (OperationCanceledException)
                {
                }
            }

            _worker = null;
            _cts?.Dispose();
            _cts = null;
        }
        finally
        {
            _stateLock.Release();
        }
    }

    private async Task MonitorConnection(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(2000, token);

                PingReply reply = await _ping.SendPingAsync("8.8.8.8");
                bool newState = reply.Status == IPStatus.Success;

                if (newState != IsConnected)
                {
                    IsConnected = newState;
                    RaiseConnectionChanged(newState);
                }
            }
            catch
            {
                if (IsConnected)
                {
                    IsConnected = false;
                    RaiseConnectionChanged(false);
                }
            }
        }
    }

    private void RaiseConnectionChanged(bool state)
    {
        if (_context != null)
        {
            _context.Post(_ => ConnectionChanged?.Invoke(this, state), null);
        }
        else
        {
            ConnectionChanged?.Invoke(this, state);
        }
    }
}
