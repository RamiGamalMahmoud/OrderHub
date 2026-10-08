using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using OrderHub.Api;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "OrderHub API";
});

ApiConfiguration.ConfigureServices(builder);

WebApplication app = builder.Build();

ApiConfiguration.ConfigureApplication(app);

app.Run();
