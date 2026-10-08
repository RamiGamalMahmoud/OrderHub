using Microsoft.AspNetCore.Authentication.Cookies;
using MudBlazor.Services;
using OrderHub.Application;
using OrderHub.Infrastructure.DependencyInjection;
using OrderHub.Infrastructure.Options;
using OrderHub.Web.Components;
using OrderHub.Web.Infrastructure;
using OrderHub.Web.Infrastructure.Authorization;
using OrderHub.Web.Services;

var builder = WebApplication.CreateBuilder(args);

if (args.Contains("--migrate", StringComparer.OrdinalIgnoreCase))
{
    // 1. Create backup

    // 2. Check pending migrations

    // 3. Apply migrations

    return;
}

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

if (builder.Environment.IsProduction())
{
    ServerConfiguration.Add(builder.Configuration);
}

builder.Services.AddApplication();
string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured.");

builder.Services.AddOptions<ApplicationDataOptions>()
    .Bind(builder.Configuration.GetSection(ApplicationDataOptions.SECTION_NAME))
    .Validate(options =>
        !string.IsNullOrWhiteSpace(options.RootPath),
        "Application data root path is required.")
    .ValidateOnStart();

builder.Services.AddInfrastructure(connectionString);

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

builder.Services.AddPermissionAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddEditorServices();


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

app.UseMiddleware<AuthorizationDeniedMiddleware>();

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
