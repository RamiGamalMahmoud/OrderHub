using FluentAssertions;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Tests.Infrastructure.Services;

public class EncryptionServiceTests
{
    [Fact]
    public void EncryptAndDecrypt()
    {
        EncryptionService encryptionService = new EncryptionService();
        string text = "this-is-my-test-data";
        byte[] encrypted = encryptionService.Encrypt(text);
        string decrypted = encryptionService.Decrypt(encrypted);

        decrypted.Should().Be(text);
    }
}
