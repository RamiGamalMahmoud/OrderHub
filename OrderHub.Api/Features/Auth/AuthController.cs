using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Configuration;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OrderHub.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IDatabaseConfigurationService _databaseConfigurationService;
    private readonly IHashShervice _hashShervice;

    public AuthController(
        IUserRepository userRepository,
        IDatabaseConfigurationService databaseConfigurationService,
        IHashShervice hashShervice)
    {
        _userRepository = userRepository;
        _databaseConfigurationService = databaseConfigurationService;
        _hashShervice = hashShervice;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        User user = await AuthenticateUserAsync(request);

        if (user is null)
            return Unauthorized("بيانات تسجيل الدخول غير صحيحة.");

        DatabaseInfo databaseInfo = CreateDatabaseInfo();

        if (databaseInfo is null)
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "لم يتم إعداد اتصال قاعدة البيانات على السيرفر.");

        AuthResponse response = CreateAuthResponse(user, databaseInfo);

        return Ok(response);
    }

    private async Task<User> AuthenticateUserAsync(LoginRequest request)
    {
        User user = await _userRepository.GetByUsernameAsync(request.UserName);

        if (user is null)
            return null;

        if (!_hashShervice.Verify(request.Password, user.PasswordHash))
            return null;

        return user;
    }

    private DatabaseInfo CreateDatabaseInfo()
    {
        try
        {
            DatabaseSettings settings = _databaseConfigurationService.GetSettings();

            IPAddress localIpAddress = HttpContext.Connection.LocalIpAddress;

            if (localIpAddress is null)
                return null;

            string host = localIpAddress.MapToIPv4().ToString();

            return new DatabaseInfo(
                host,
                settings.Port,
                settings.Database,
                settings.Username,
                settings.Password);
        }
        catch
        {
            return null;
        }
    }

    private static AuthResponse CreateAuthResponse(
        User user,
        DatabaseInfo databaseInfo)
    {
        UserInfo userInfo = new(
            user.Id,
            user.UserName,
            user.DisplayName,
            user.IsAdmin,
            user.Permissions
                .Select(CreateUserPermissionInfo)
                .ToList());

        return new AuthResponse(
            databaseInfo,
            userInfo);
    }

    private static UserPermissionInfo CreateUserPermissionInfo(
        UserPermission permission)
    {
        return new UserPermissionInfo(
            permission.Feature,
            permission.CanRead,
            permission.CanWrite);
    }
}

public sealed record LoginRequest(
    string UserName,
    string Password);

public sealed record AuthResponse(
    DatabaseInfo DatabaseInfo,
    UserInfo UserInfo);

public sealed record DatabaseInfo(
    string Host,
    int Port,
    string Database,
    string Username,
    string Password);

public sealed record UserInfo(
    int Id,
    string UserName,
    string DisplayName,
    bool IsAdmin,
    IReadOnlyCollection<UserPermissionInfo> UserPermissions);

public sealed record UserPermissionInfo(
    PermissionFeature Feature,
    bool CanRead,
    bool CanWrite);