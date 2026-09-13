using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Configuration;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Api.Controllers;

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
        User? user = await _userRepository.GetByUsernameAsync(request.UserName);

        if (user is null)
            return Unauthorized("بيانات تسجيل الدخول غير صحيحة.");

        if (!_hashShervice.Verify(request.Password, user.PasswordHash))
            return Unauthorized("بيانات تسجيل الدخول غير صحيحة.");

        DatabaseSettings? databaseSettings;
        try
        {
            databaseSettings = _databaseConfigurationService.GetSettings();
        }
        catch
        {
            return StatusCode(503, "لم يتم إعداد اتصال قاعدة البيانات على السيرفر.");
        }

        if (databaseSettings is null)
            return StatusCode(503, "لم يتم إعداد اتصال قاعدة البيانات على السيرفر.");

        AuthResponse response = new(
            new DatabaseInfo(
                databaseSettings.Host,
                databaseSettings.Port,
                databaseSettings.Database,
                databaseSettings.Username,
                databaseSettings.Password),

            new UserInfo(
                user.Id,
                user.UserName,
                user.DisplayName,
                user.IsActive));

        return Ok(response);
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
    bool IsActive);
