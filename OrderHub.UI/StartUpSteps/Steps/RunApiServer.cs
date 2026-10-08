//using OrderHub.Application.Infrastructure.Installation;
//using OrderHub.UI.Services.ApiServer;
//using System.Threading.Tasks;

//namespace OrderHub.UI.StartUpSteps.Steps;
//internal class RunApiServer : IStartupStep
//{
//    private readonly IApplicationConfigurationProvider _typeProvider;
//    private readonly IApiServerService _apiServerService;

//    public RunApiServer(IApplicationConfigurationProvider typeProvider, IApiServerService apiServerService)
//    {
//        _typeProvider = typeProvider;
//        _apiServerService = apiServerService;
//    }

//    public int Order => (int)StartUpdStepsOrder.RunApiServer;
//    public string DisplayName => "تشغيل سيرفر ال Api";
//    public bool IsEnabled
//    {
//        get
//        {
//#if DEBUG
//            return true;
//#else
//            return _typeProvider.GetConfiguration().InstallationType == InstallationType.Server;
//#endif
//        }
//    }

//    public Task ExecuteAsync()
//    {
//         return _apiServerService.StartAsync();
//    }
//}
