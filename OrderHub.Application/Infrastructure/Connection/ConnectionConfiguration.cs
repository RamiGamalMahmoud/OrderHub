namespace OrderHub.Application.Infrastructure.Connection;

public sealed record ConnectionConfiguration(
    string Host,
    int Port,
    string Database,
    string Username,
    string Password);