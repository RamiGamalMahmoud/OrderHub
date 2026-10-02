using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using OrderHub.Application.Features.Authentication.DatabaseLogin;
using OrderHub.Application.Interfaces;
using System.Security.Claims;

namespace OrderHub.Web.Controllers;

[Route("/auth")]
public class Auth : Controller
{
    private readonly IRequestExecutor _requestExecutor;

    public Auth(IRequestExecutor requestExecutor)
    {
        _requestExecutor = requestExecutor;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] LoginRequest request, CancellationToken cancellationToken)
    {
        var userLoginResult = await _requestExecutor.ExecuteAsync(new DatabaseLoginCommand(
            request.Username,
            request.Password,
            request.RememberMe));

        if (!userLoginResult.IsSuccess)
            return Redirect("/login?error=invalid");

        var user = userLoginResult.Value;

        List<Claim> claims =
            [
                new (ClaimTypes.NameIdentifier, user.Id.ToString()),
                new (ClaimTypes.Name, user.UserName),
                new ("DisplayName", user.DisplayName),
            ];

        if (user.IsAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var properties = new AuthenticationProperties()
        {
            IsPersistent = request.RememberMe
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            properties);

        return Redirect("/");
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect("/login");
    }
}

public sealed record LoginRequest(
    string Username,
    string Password,
    bool RememberMe);