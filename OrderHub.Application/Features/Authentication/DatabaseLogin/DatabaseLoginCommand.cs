using MediatR;
using OrderHub.Application.Context;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Authentication.DatabaseLogin;
public record DatabaseLoginCommand(
    string UserName, 
    string Password, 
    bool RememberMe) : IRequest<Result<UserSession>>;
