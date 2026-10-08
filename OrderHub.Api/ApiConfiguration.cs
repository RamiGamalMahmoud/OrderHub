using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrderHub.Api.Interfaces;

namespace OrderHub.Api;

public static class ApiConfiguration
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddApi(builder.Configuration);
        builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(5000));
    }

    public static void ConfigureApplication(WebApplication app)
    {
        IFileStorageDirectories directories =
            app.Services.GetRequiredService<IFileStorageDirectories>();

        directories.EnsureDirectoriesCreated();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthorization();

        app.MapControllers();
    }
}