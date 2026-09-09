using System.Collections.Generic;

namespace OrderHub.Application.Features.Authentication.ApiLogin;
public record AuthResponse(
    DatabaseInfo DatabaseInfo,
    UserInfo UserInfo);

public sealed record DatabaseInfo(
    string Host,
    int Port,
    string Database,
    string UserName,
    string Password);

public sealed record UserInfo(
    int Id,
    string UserName,
    string DisplayName,
    IReadOnlyCollection<string> Roles);
