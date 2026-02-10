using MediatR;
using OrderHub.Application.Commands;
using OrderHub.Application.DTOs;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.CommandHandlers.Auth;

internal class AuthCommandHandler(ITokenStorageService tokenStorageService,
                                  ISessionManager sessionManager,
                                  IAuthService authService,
                                  ICredentialsService credentialsService) : IRequestHandler<AuthCommand, bool>
{
    private readonly ITokenStorageService _tokenStorageService = tokenStorageService;
    private readonly ISessionManager _sessionManager = sessionManager;
    private readonly IAuthService _authService = authService;
    private readonly ICredentialsService _credentialsService = credentialsService;

    public async Task<bool> Handle(AuthCommand request, CancellationToken cancellationToken)
    {
        ClientCredentials clientCredentials = await _credentialsService.GetClilentCredentialsAsync();
        if (clientCredentials is null)
        {
            return false;
        }

        Token savedToken = await _tokenStorageService.GetTokenAsync();

        if (savedToken is not null && !savedToken.IsExpired)
        {
            return true;
        }

        await _sessionManager.StartNewSession();
        return true;
    }
}
