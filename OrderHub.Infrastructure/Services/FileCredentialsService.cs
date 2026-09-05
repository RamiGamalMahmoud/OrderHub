using OrderHub.Application.DTOs;
using OrderHub.Application.Interfaces.Services;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;

internal class FileCredentialsService : ICredentialsService
{
    private readonly IApplicationDirectoriesService _applicationDirectoriesService;
    private readonly IEncryptionService _encryptionService;

    public FileCredentialsService(
        IApplicationDirectoriesService applicationDirectoriesService,
        IEncryptionService encryptionService)
    {
        _applicationDirectoriesService = applicationDirectoriesService;
        _encryptionService = encryptionService;
    }

    public void DeleteClientCredentials()
    {
        string filePath =
            _applicationDirectoriesService.CredentialsFilePath;

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public async Task<ClientCredentials> GetClilentCredentialsAsync()
    {
        string filePath =
            _applicationDirectoriesService.CredentialsFilePath;

        if (!File.Exists(filePath))
        {
            return null;
        }

        byte[] encryptedData =
            await File.ReadAllBytesAsync(filePath);

        byte[] data =
            _encryptionService.Decrypt(encryptedData);

        return JsonSerializer.Deserialize<ClientCredentials>(data);
    }

    public async Task SaveClientCredentialsAsync(
        ClientCredentials clientCredentials)
    {
        byte[] data =
            JsonSerializer.SerializeToUtf8Bytes(clientCredentials);

        byte[] encryptedData =
            _encryptionService.Encrypt(data);

        await File.WriteAllBytesAsync(
            _applicationDirectoriesService.CredentialsFilePath,
            encryptedData);
    }
}