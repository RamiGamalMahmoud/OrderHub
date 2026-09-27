using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;

namespace OrderHub.Api.Features.TelegramIntegration.Linking.Complete;
public record CompleteTelegramLinkingCommand(
    string Token,
    long TelegramUserId,
    long ChatId,
    string Username,
    string FirstName,
    string LastName) : IRequest<Result<TelegramAccount>>;
