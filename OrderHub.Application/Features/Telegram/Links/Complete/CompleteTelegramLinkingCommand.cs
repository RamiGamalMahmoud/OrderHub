using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Telegram.Links.Complete;
public record CompleteTelegramLinkingCommand(
    string Token,
    long TelegramUserId,
    long ChatId,
    string Username,
    string FirstName,
    string LastName) : IRequest<Result>;
