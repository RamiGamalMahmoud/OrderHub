namespace OrderHub.Application.Interfaces.Services
{
    public interface IEncryptionService
    {
        byte[] Encrypt(string data);
        string Decrypt(byte[] data);
    }
}
