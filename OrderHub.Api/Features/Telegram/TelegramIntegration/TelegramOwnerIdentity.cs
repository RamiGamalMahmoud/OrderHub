using OrderHub.Domain.Enums;

namespace OrderHub.Api.Features.Telegram.TelegramIntegration;

public record TelegramOwnerIdentity(TelegramAccountOwnerType OwnerType, int OwnerId);
