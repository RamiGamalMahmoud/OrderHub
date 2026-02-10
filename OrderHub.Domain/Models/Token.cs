using System;

namespace OrderHub.Domain.Models;

public class Token
{
    public string TokenType { get; }
    public string AccessToken { get; }
    public string RefreshToken { get; }
    public DateTime ExpiresAt { get; }
    public string Scope { get; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    // Add buffer time to refresh before actual expiration
    public bool IsExpiringSoon => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);

    public Token(string tokenType, string accessToken, string refreshToken,
                 DateTime expiresAt, string scope)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentException("Access token cannot be empty", nameof(accessToken));

        TokenType = tokenType;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = expiresAt;
        Scope = scope;
    }
}