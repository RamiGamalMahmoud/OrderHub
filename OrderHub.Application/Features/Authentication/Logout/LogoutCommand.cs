using MediatR;

namespace OrderHub.Application.Features.Authentication.Logout;

public sealed record LogoutCommand : IRequest;