using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrderHub.Api.Features.TelegramIntegration;
using OrderHub.Api.Interfaces;
using OrderHub.Api.Services;
using OrderHub.Application;
using OrderHub.Application.Options;
using OrderHub.Infrastructure.DependencyInjection;
using System.Reflection;
using Telegram.Bot;

namespace OrderHub.Api;

public static class SrviceCollectionExtensions
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TelegramLinkOptions>(configuration.GetSection("Telegram"));
        services.Configure<TelegramOptions>(configuration.GetSection("Telegram"));
        services.AddReceiving();


        services.AddSingleton<TelegramBotClient>(sp =>
        {
            TelegramOptions options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
            return new TelegramBotClient(options.BotToken);
        });

        services.AddSingleton<ITelegramBotClient>(sp => sp.GetRequiredService<TelegramBotClient>());

        services.AddSingleton<IFileStorageDirectories, FileStorageDirectories>();
        services.AddSingleton<ILocalFileAccessService, LocalFileAccessService>();

        services.AddControllers().AddApplicationPart(typeof(ApiConfiguration).Assembly);

        services.AddOpenApi();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        services.AddInfrastructure();

        services.AddApplication();
        return services;
    }
}
