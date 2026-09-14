using OrderHub.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps.Steps;
public sealed class StartConnectionServiceStep : IStartupStep
{
    private readonly IConnectionService _connectionService;

    public StartConnectionServiceStep(IConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    public int Order => (int)StartUpdStepsOrder.ConnectionStep;
    public string DisplayName => "التحقق من الاتصال.";
    public bool IsEnabled => true;

    public async Task ExecuteAsync()
    {
        await _connectionService.StartAsync();
    }
}