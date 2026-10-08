using OrderHub.Domain.Models;
using Telegram.Bot.Types;

namespace OrderHub.Api.Features.Telegram.TelegramIntegration;

public sealed class TelegramUpdateContext
{
    public TelegramUpdateContext(
        Update update,
        long? telegramUserId,
        TelegramAccount account)
    {
        Update = update;
        TelegramUserId = telegramUserId;
        Account = account;

        Owner = account is not null
            ? new TelegramOwnerIdentity(
                account.OwnerType,
                account.OwnerId)
            : null;
    }

    public Update Update { get; }

    public long? TelegramUserId { get; }

    public TelegramAccount Account { get; }

    public TelegramOwnerIdentity Owner { get; }

    public bool IsAuthenticated => Account is not null;
}