using System.Text.Json.Serialization;

namespace OrderHub.Application.DTOs;

public record ClientCredentials
(
    [property: JsonPropertyName("ClientId")]
    string ClientId,
    [property: JsonPropertyName("ClientSecret")]
    string ClientSecret
);
