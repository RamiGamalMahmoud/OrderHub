using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrderHub.Api.Features.Telegram;
using OrderHub.Api.Features.Telegram.Receiving;
using OrderHub.Api.Features.Telegram.Services;
using OrderHub.Api.Interfaces;
using OrderHub.Api.Services;
using OrderHub.Application;
using OrderHub.Application.Options;
using OrderHub.Infrastructure.DependencyInjection;
using System;

namespace OrderHub.Api;

public static class SrviceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TelegramLinkOptions>(configuration.GetSection("Telegram"));
        services.Configure<TelegramOptions>(configuration.GetSection("Telegram"));
        services.AddReceiving();
        services.AddHttpClient<ITelegramHttpClient, TelegramHttpClient>((sp, client) =>
        {
            TelegramOptions options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
            client.BaseAddress = new Uri($"https://api.telegram.org/bot{options.BotToken}/");
        });

        services.AddSingleton<IFileStorageDirectories, FileStorageDirectories>();
        services.AddSingleton<ILocalFileAccessService, LocalFileAccessService>();

        services.AddControllers().AddApplicationPart(typeof(ApiConfiguration).Assembly);

        services.AddOpenApi();

        services.AddInfrastructure();

        services.AddApplication();
        return services;
    }
}
