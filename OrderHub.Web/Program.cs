using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Localization;
using OrderHub.Application;
using OrderHub.Infrastructure.DependencyInjection;
using OrderHub.Infrastructure.Options;
using OrderHub.Web.Components;
using OrderHub.Web.Infrastructure.Authorization;
using OrderHub.Web.Infrastructure.DependencyInjection;
using QuestPDF.Infrastructure;
using TailwindBlazor;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;
QuestPDF.Settings.UseSystemFonts = true;

// =========================
// Services
// =========================

builder.UseTailwind();

builder.Services.AddWebServices();
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

// Blazor circuits swallow exceptions by default; trace them fully while developing.
if (builder.Environment.IsDevelopment())
{
    builder.Services.Configure<CircuitOptions>(options => options.DetailedErrors = true);
}

// =========================
// Build
// =========================

var app = builder.Build();


// =========================
// HTTP Pipeline
// =========================

// Deliberate: HTTPS redirection is disabled for the offline/LAN Windows-service
// deployment; see the SecurePolicy comment in AuthenticationRegistration.cs.

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Pin the request culture so digits/labels never depend on the OS culture of the
// service account (local code formats Latin digits explicitly with InvariantCulture).
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("ar"),
    SupportedCultures = [new("ar")],
    SupportedUICultures = [new("ar")],
    // No request providers: every request deterministically uses the default culture.
});

app.UseStatusCodePagesWithReExecute("/not-found");

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
