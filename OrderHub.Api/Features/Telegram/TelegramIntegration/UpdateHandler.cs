using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderHub.Api.Features.Telegram.TelegramIntegration;
using OrderHub.Api.Features.Telegram.TelegramIntegration.Routing;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace OrderHub.Api.Features.TelegramIntegration;

public sealed class UpdateHandler : IUpdateHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<UpdateHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();

        IEnumerable<IUpdateRouteHandler> handlers =
            scope.ServiceProvider
                .GetRequiredService<IEnumerable<IUpdateRouteHandler>>();

        IUpdateRouteHandler handler =
            handlers.FirstOrDefault(x => x.CanHandle(update));

        if (handler is null)
        {
            _logger.LogWarning(
                "No route handler found for Telegram update {UpdateId} of type {UpdateType}.",
                update.Id,
                update.Type);

            return;
        }

        TelegramUpdateContext context = await BuildContextAsync(
            update,
            handler,
            scope);
        
        await handler.HandleAsync(context, cancellationToken);
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        HandleErrorSource source,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Telegram receiving error. Source: {Source}.",
            source);

        return Task.CompletedTask;
    }

    private async Task<TelegramUpdateContext> BuildContextAsync(
        Update update,
        IUpdateRouteHandler handler,
        IServiceScope scope)
    {
        long? telegramUserId = handler.GetTelegramUserId(update);

        TelegramAccount telegramAccount = null;

        if (telegramUserId.HasValue)
        {
            ITelegramAccountRepository accountRepository = scope.ServiceProvider.GetRequiredService<ITelegramAccountRepository>();

            telegramAccount = await accountRepository.GetByTelegramUserId(telegramUserId.Value);
        }

        return new TelegramUpdateContext(
            update,
            telegramUserId,
            telegramAccount);
    }
}