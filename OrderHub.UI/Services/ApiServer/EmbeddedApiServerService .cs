//using Microsoft.AspNetCore.Builder;
//using OrderHub.Api;
//using System.Diagnostics;
//using System.Threading;
//using System.Threading.Tasks;

//namespace OrderHub.UI.Services.ApiServer;
//internal sealed class EmbeddedApiServerService : IApiServerService
//{
//    private WebApplication _application;

//    public bool IsRunning => _application is not null;

//    public async Task StartAsync(CancellationToken cancellationToken = default)
//    {
//        if (_application is not null)
//            return;

//        WebApplicationBuilder builder = WebApplication.CreateBuilder();

//        ApiConfiguration.ConfigureServices(builder);

//        _application = builder.Build();

//        ApiConfiguration.ConfigureApplication(_application);

//        await _application.StartAsync(cancellationToken);

//        foreach(string address in _application.Urls)
//        {
//            Debug.WriteLine($"Api listening: {address}");
//        }
//    }

//    public async Task StopAsync(CancellationToken cancellationToken = default)
//    {
//        if (_application is null)
//            return;

//        await _application.StopAsync(cancellationToken);
//        await _application.DisposeAsync();

//        _application = null;
//    }

//    public async Task RestartAsync(CancellationToken cancellationToken = default)
//    {
//        await StopAsync(cancellationToken);
//        await StartAsync(cancellationToken);
//    }
//}