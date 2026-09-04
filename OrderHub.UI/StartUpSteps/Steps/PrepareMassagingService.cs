using OrderHub.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps.Steps;

internal class PrepareMessagingService : IStartupStep
{
    public int Order => (int)StartUpdStepsOrder.PrepareDirectories;

    public string DisplayName => "تجهيز خدمة الرسائل";

    public bool IsEnabled => true;

    private readonly IConnectionService _connectionService;
    private readonly IWppConnectScriptService _wppConnectScriptService;

    public PrepareMessagingService(
        IConnectionService connectionService,
        IWppConnectScriptService wppConnectScriptService)
    {
        _connectionService = connectionService;
        _wppConnectScriptService = wppConnectScriptService;
    }

    public async Task ExecuteAsync()
    {
        if (!_connectionService.IsConnected)
            return;

        await _wppConnectScriptService.PrepareAsync();
    }
}