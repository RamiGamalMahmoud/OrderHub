using OrderHub.Application.DTOs;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services
{
    public interface ICredentialsService
    {
        Task<ClientCredentials> GetClilentCredentialsAsync();
        Task SaveClientCredentialsAsync(ClientCredentials clientCredentials);
        void DeleteClientCredentials();
    }
}
