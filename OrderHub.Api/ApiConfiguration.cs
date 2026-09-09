using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderHub.Application;
using OrderHub.Infrastructure.DependencyInjection;

namespace OrderHub.Api;

public static class ApiConfiguration
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder
            .Services
            .AddControllers()
            .AddApplicationPart(typeof(ApiConfiguration).Assembly);

        builder
            .Services
            .AddOpenApi();

        builder
            .Services
            .AddInfrastructure();

        builder
            .Services
            .AddApplication();


        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(5000));
    }

    public static void ConfigureApplication(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseAuthorization();
        app.MapControllers();
    }
}