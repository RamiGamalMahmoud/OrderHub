using OrderHub.Application.Interfaces.Services;
using System.Security.Cryptography;

namespace OrderHub.Infrastructure.Services
{
    internal class EncryptionService : IEncryptionService
    {
        private static readonly byte[] _key = null;

        public EncryptionService()
        {
            //string key = "YT783NDFTB3S7HMSH53DXHDV9T96TR8V";
            //byte[] bytes = System.Text.Encoding.UTF8.GetBytes(key);
        }

        public byte[] Encrypt(string data) => Protect(Encode(data));

        public string Decrypt(byte[] data) => Decode(Unprotect(data));

        private static byte[] Encode(string data) => System.Text.Encoding.UTF8.GetBytes(data);
        private static string Decode(byte[] data) => System.Text.Encoding.UTF8.GetString(data);
        private static byte[] Protect(byte[] data) => ProtectedData.Protect(data, _key, DataProtectionScope.CurrentUser);
        private static byte[] Unprotect(byte[] bytes) => ProtectedData.Unprotect(bytes, _key, DataProtectionScope.CurrentUser);
    }
}
