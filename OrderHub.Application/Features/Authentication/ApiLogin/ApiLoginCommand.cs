using MediatR;
using OrderHub.Domain.Common;

namespace OrderHub.Application.Features.Authentication.ApiLogin;
public record ApiLoginCommand(
    string UserName,
    string Password,
    bool RememberMe = false) : IRequest<Result<AuthResponse>>;
