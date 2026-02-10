using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services
{
    internal class FieTokenStorageService : ITokenStorageService
    {
        private readonly IApplicationDirectoriesService _applicationDirectoriesService;
        private readonly IEncryptionService _encryptionService;

        public FieTokenStorageService(IApplicationDirectoriesService applicationDirectoriesService, IEncryptionService encryptionService)
        {
            _applicationDirectoriesService = applicationDirectoriesService;
            _encryptionService = encryptionService;
        }

        public Task ClearTokenAsync()
        {
            throw new System.NotImplementedException();
        }

        public async Task<Token> GetTokenAsync()
        {
            string tokenFilePath = _applicationDirectoriesService.TokenFilePath;
            if (!File.Exists(tokenFilePath))
            {
                return null;
            }

            byte[] fileContents = await File.ReadAllBytesAsync(tokenFilePath);
            string decryptedContents = _encryptionService.Decrypt(fileContents);
            Token token = JsonSerializer.Deserialize<Token>(decryptedContents);
            return token;
        }

        public async Task SaveTokenAsync(Token token)
        {
            string tokenJson = JsonSerializer.Serialize(token);
            byte[] encrypted = _encryptionService.Encrypt(tokenJson);
            await File.WriteAllBytesAsync(_applicationDirectoriesService.TokenFilePath, encrypted);
        }
    }
}
