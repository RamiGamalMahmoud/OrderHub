using System;

namespace OrderHub.Application.Interfaces.Services;

public interface IConnectionService
{
    event EventHandler<bool> ConnectionChanged;
    bool IsConnected { get; }
    bool TryConnect();
    void Start();
    void Stop();
}
