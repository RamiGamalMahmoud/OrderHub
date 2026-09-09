namespace OrderHub.Application.Features.Authentication.DatabaseLogin;
public sealed record UserInfoDto(
    int Id,
    string UserName,
    string DisplayName);
