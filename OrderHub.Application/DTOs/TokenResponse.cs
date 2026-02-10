using System.Text.Json.Serialization;

namespace OrderHub.Application.DTOs;

public record TokenResponse
(
    [property: JsonPropertyName("token_type")]
    string TokenType,

    [property: JsonPropertyName("access_token")]
    string AccessToken,

    [property: JsonPropertyName("refresh_token")]
    string RefreshToken,

    [property: JsonPropertyName("expires_in")]
    int ExpiresIn,

    [property: JsonPropertyName("scope")]
    string Scope
);
