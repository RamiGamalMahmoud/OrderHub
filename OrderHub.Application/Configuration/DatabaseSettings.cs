namespace OrderHub.Application.Configuration;

public sealed class DatabaseSettings
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5432;
    public string Database { get; init; } = "orderhub";
    public string Username { get; init; } = "postgres";
    public string Password { get; init; } = "12345678";
}