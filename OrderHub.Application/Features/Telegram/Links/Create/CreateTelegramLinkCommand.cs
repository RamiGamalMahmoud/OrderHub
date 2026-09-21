using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Application.Features.Telegram.Links.Create;
public record CreateTelegramLinkCommand(CreateTelegramLinkRequest Request)
    : IRequest<Result<CreateTelegramLinkResult>>;

public record CreateTelegramLinkRequest(
    TelegramAccountOwnerType OwnerType,
    int OwnerId);

public record CreateTelegramLinkResult(
    string Link,
    DateTime ExpiresAt);
