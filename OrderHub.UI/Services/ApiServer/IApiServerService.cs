using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.UI.Services.ApiServer;
public interface IApiServerService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task RestartAsync(CancellationToken cancellationToken = default);
    bool IsRunning { get; }
}