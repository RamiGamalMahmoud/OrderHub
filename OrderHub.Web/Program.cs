using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
using OrderHub.Application;
using OrderHub.Domain;
using Microsoft.Extensions.Hosting;
using OrderHub.Infrastructure.DependencyInjection;
using OrderHub.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// =========================
// Services
// =========================

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "OrderHubWeb";
});

builder.WebHost.UseUrls("http://0.0.0.0:5000");

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddDomain();

builder.Services.AddMudServices();

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";

        options.Cookie.Name = "OrderHub.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();


// =========================
// Build
// =========================

var app = builder.Build();


// =========================
// HTTP Pipeline
// =========================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseStatusCodePagesWithRedirects("/not-found");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();


// =========================
// Endpoints
// =========================

app.MapStaticAssets();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
