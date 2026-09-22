using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Configuration;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System;
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
    private readonly IAppLogger _appLogger;

    public AuthController(
        IUserRepository userRepository,
        IDatabaseConfigurationService databaseConfigurationService,
        IHashShervice hashShervice,
        IAppLogger appLogger)
    {
        _userRepository = userRepository;
        _databaseConfigurationService = databaseConfigurationService;
        _hashShervice = hashShervice;
        _appLogger = appLogger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            await _appLogger.LogInfoAsync(
                $"[Auth] Login started. UserName: {request.UserName}");

            await _appLogger.LogInfoAsync(
                $"[Auth] Windows Identity: {System.Security.Principal.WindowsIdentity.GetCurrent().Name}");

            // 1. Authenticate
            await _appLogger.LogInfoAsync(
                "[Auth] Before AuthenticateUserAsync.");

            User user = await AuthenticateUserAsync(request);

            await _appLogger.LogInfoAsync(
                $"[Auth] After AuthenticateUserAsync. User found: {user is not null}");

            if (user is null)
            {
                await _appLogger.LogInfoAsync(
                    "[Auth] Authentication failed.");

                return Unauthorized("بيانات تسجيل الدخول غير صحيحة.");
            }

            // 2. Database configuration
            await _appLogger.LogInfoAsync(
                "[Auth] Before CreateDatabaseInfo.");

            DatabaseInfo databaseInfo = await CreateDatabaseInfoAsync();

            await _appLogger.LogInfoAsync(
                $"[Auth] After CreateDatabaseInfo. Found: {databaseInfo is not null}");

            if (databaseInfo is null)
            {
                await _appLogger.LogErrorAsync(
                    "[Auth] DatabaseInfo could not be created.");

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    "لم يتم إعداد اتصال قاعدة البيانات على السيرفر.");
            }

            // 3. Response
            await _appLogger.LogInfoAsync(
                "[Auth] Before CreateAuthResponse.");

            AuthResponse response = CreateAuthResponse(user, databaseInfo);

            await _appLogger.LogInfoAsync(
                "[Auth] Login completed successfully.");

            return Ok(response);
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync(
                "[Auth] Unhandled exception during login.",
                ex);

            throw;
        }
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

    private async Task<DatabaseInfo> CreateDatabaseInfoAsync()
    {
        try
        {
            await _appLogger.LogInfoAsync(
                "[Auth] Reading database settings.");

            DatabaseSettings settings =
                _databaseConfigurationService.GetSettings();

            if (settings is null)
            {
                await _appLogger.LogErrorAsync(
                    "[Auth] Database settings are null.");

                return null;
            }

            await _appLogger.LogInfoAsync(
                "[Auth] Database settings loaded successfully.");

            IPAddress localIpAddress =
                HttpContext.Connection.LocalIpAddress;

            if (localIpAddress is null)
            {
                await _appLogger.LogErrorAsync(
                    "[Auth] LocalIpAddress is null.");

                return null;
            }

            string host = localIpAddress
                .MapToIPv4()
                .ToString();

            await _appLogger.LogInfoAsync(
                $"[Auth] Local IP resolved: {host}");

            return new DatabaseInfo(
                host,
                settings.Port,
                settings.Database,
                settings.Username,
                settings.Password);
        }
        catch (Exception ex)
        {
            await _appLogger.LogErrorAsync(
                "[Auth] CreateDatabaseInfo failed.",
                ex);

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