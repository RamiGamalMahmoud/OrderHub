using OrderHub.Application.DTOs;
using OrderHub.Application.Interfaces.Services;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services
{
    internal class FileCredentialsService : ICredentialsService
    {
        private readonly IApplicationDirectoriesService _applicationDirectoriesService;
        private readonly IEncryptionService _encryptionService;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public FileCredentialsService(IApplicationDirectoriesService applicationDirectoriesService, IEncryptionService encryptionService)
        {
            _applicationDirectoriesService = applicationDirectoriesService;
            _encryptionService = encryptionService;
        }

        public void DeleteClientCredentials()
        {
            if(File.Exists(_applicationDirectoriesService.CredentialsFilePath))
            {
                File.Delete(_applicationDirectoriesService.CredentialsFilePath);
            }
        }

        public async Task<ClientCredentials> GetClilentCredentialsAsync()
        {
            if (!File.Exists(_applicationDirectoriesService.CredentialsFilePath))
            {
                return null;
            }

            byte[] fileContents = await File.ReadAllBytesAsync(_applicationDirectoriesService.CredentialsFilePath);
            string decryptedContents = _encryptionService.Decrypt(fileContents);
            ClientCredentials clientCredentials = JsonSerializer.Deserialize<ClientCredentials>(decryptedContents);
            return clientCredentials;
        }

        public async Task SaveClientCredentialsAsync(ClientCredentials clientCredentials)
        {
            byte[] encryptedContents = _encryptionService.Encrypt(JsonSerializer.Serialize(clientCredentials));
            await File.WriteAllBytesAsync(_applicationDirectoriesService.CredentialsFilePath, encryptedContents);
        }
    }
}
