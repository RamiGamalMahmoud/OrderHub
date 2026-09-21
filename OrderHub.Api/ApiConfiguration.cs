using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OrderHub.Api.Features.Telegram;
using OrderHub.Api.Features.Telegram.Receiving;
using OrderHub.Api.Features.Telegram.Services;
using OrderHub.Application;
using OrderHub.Application.Options;
using OrderHub.Infrastructure.DependencyInjection;
using System;

namespace OrderHub.Api;

public static class ApiConfiguration
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.Configure<TelegramLinkOptions>(builder.Configuration.GetSection("Telegram"));
        builder.Services.Configure<TelegramOptions>(builder.Configuration.GetSection("Telegram"));
        builder.Services.AddReceiving();
        builder.Services.AddHttpClient<ITelegramHttpClient, TelegramHttpClient>((sp, client) =>
        {
            TelegramOptions options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
            client.BaseAddress = new Uri($"https://api.telegram.org/bot{options.BotToken}/");
        });

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