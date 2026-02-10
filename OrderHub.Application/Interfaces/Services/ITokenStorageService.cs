using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface ITokenStorageService
{
    Task<Token> GetTokenAsync();
    Task SaveTokenAsync(Token token);
    Task ClearTokenAsync();
}
