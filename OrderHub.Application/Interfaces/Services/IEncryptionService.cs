namespace OrderHub.Application.Interfaces.Services;

public interface IEncryptionService
{
    byte[] Encrypt(byte[] data);

    byte[] Decrypt(byte[] data);
}