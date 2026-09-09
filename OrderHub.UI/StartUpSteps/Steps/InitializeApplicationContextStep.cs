using OrderHub.Application.Context;
using OrderHub.Application.Infrastructure.Installation;
using System.Threading.Tasks;

namespace OrderHub.UI.StartUpSteps.Steps;
internal class InitializeApplicationContextStep : IStartupStep
{
    private readonly IApplicationConfigurationProvider _typeProvider;
    private readonly IApplicationContext _applicationContext;

    public InitializeApplicationContextStep(IApplicationConfigurationProvider typeProvider, IApplicationContext applicationContext)
    {
        _typeProvider = typeProvider;
        _applicationContext = applicationContext;
    }

    public int Order => (int)StartUpdStepsOrder.InitializeApplicationContextStep;
    public string DisplayName => "تهيئة التطبيق";
    public bool IsEnabled => true;

    public Task ExecuteAsync()
    {
        ApplicationConfiguration installation =  _typeProvider.GetConfiguration();
        _applicationContext.Initialize(new ApplicationInfo(installation.InstallationType, installation.Version));

        return Task.CompletedTask;
    }
}
