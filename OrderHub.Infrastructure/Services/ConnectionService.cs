using OrderHub.Application.Interfaces.Services;
using System;
using System.Net.NetworkInformation;

namespace OrderHub.Infrastructure.Services;

internal class ConnectionService : IConnectionService
{
    System.Timers.Timer _timer;

    private readonly Ping _ping = new Ping();

    public ConnectionService()
    {
        _timer = new System.Timers.Timer
        {
            Interval = 1000
        };
    }

    public bool IsConnected { get; private set; }

    public event EventHandler<bool> ConnectionChanged;

    public bool TryConnect()
    {
        try
        {
            PingReply reply = _ping.Send("8.8.8.8");
            IsConnected = reply.Status == IPStatus.Success;
        }
        catch (Exception)
        {
            IsConnected = false;
        }
        return IsConnected;
    }

    public void Start()
    {
        _timer.Elapsed += (sender, args) =>
            {
                ConnectionChanged?.Invoke(this, TryConnect());
            };
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Elapsed -= (sender, args) => ConnectionChanged?.Invoke(this, TryConnect());
    }
}
