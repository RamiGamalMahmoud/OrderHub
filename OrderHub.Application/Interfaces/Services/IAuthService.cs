using OrderHub.Application.DTOs;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface IAuthService
{
    Task<Result<Token>> AuthorizeAsync(ClientCredentials clientCredentials);
    Task<Result<Token>> GetAccessTokenAsync(ClientCredentials clientCredentials, string code);
    Task<Result<Token>> RefreshTokenAsync(ClientCredentials clientCredentials, Token token);
}
