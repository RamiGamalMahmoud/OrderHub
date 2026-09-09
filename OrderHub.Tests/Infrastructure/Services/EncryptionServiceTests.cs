using FluentAssertions;
using OrderHub.Infrastructure.Services;
using System.Text;

namespace OrderHub.Tests.Infrastructure.Services;

public class EncryptionServiceTests
{
    [Fact]
    public void EncryptAndDecrypt()
    {
        EncryptionService encryptionService = new EncryptionService();
        string text = "this-is-my-test-data";
        byte[] textBytes = Encoding.UTF8.GetBytes(text);
        byte[] encrypted = encryptionService.Encrypt(textBytes);
        
        byte[] decryptedBytes = encryptionService.Decrypt(encrypted);
        string decryptedString = Encoding.UTF8.GetString(decryptedBytes);

        decryptedString.Should().Be(text);
    }
}
