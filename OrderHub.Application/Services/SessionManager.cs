using OrderHub.Application.DTOs;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Application.Services
{
    internal class SessionManager : ISessionManager
    {
        private readonly ICredentialsService _credentialsService;
        private readonly ITokenStorageService _tokenStorageService;
        private readonly IAuthService _authService;

        public SessionManager(
            ICredentialsService credentialsService,
            ITokenStorageService tokenStorageService,
            IAuthService authService)
        {
            _credentialsService = credentialsService;
            _tokenStorageService = tokenStorageService;
            _authService = authService;
        }

        public Session CurrentSession { get; private set; }

        public async Task StartNewSession()
        {
            ClientCredentials clientCredentials = await _credentialsService.GetClilentCredentialsAsync();

            Token savedToken = await _tokenStorageService.GetTokenAsync();

            if (savedToken is null || string.IsNullOrEmpty(savedToken.AccessToken))
            {
                Result<Token> tokenResult = await _authService.AuthorizeAsync(clientCredentials);
                if (!tokenResult.IsSuccess)
                    return;

                await _tokenStorageService.SaveTokenAsync(tokenResult.Value);
                CurrentSession = new Session(tokenResult.Value);
                return;
            }

            else if (savedToken.IsExpired || savedToken.IsExpiringSoon)
            {
                Result<Token> tokenResult = await _authService.RefreshTokenAsync(clientCredentials, savedToken);
                
                if (!tokenResult.IsSuccess)
                    return;

                await _tokenStorageService.SaveTokenAsync(tokenResult.Value);
                CurrentSession = new Session(tokenResult.Value);
                return;
            }

            CurrentSession = new Session(savedToken);
        }
    }
}
