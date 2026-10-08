using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderHub.Application;
using OrderHub.Application.Authorization;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Web.Infrastructure.Authorization;
using OrderHub.Web.Services;
using AuthorizationTests = OrderHub.Tests.Application.AuthorizationTests;

namespace OrderHub.Tests.Web;

public class PermissionAuthorizationTests
{
    [Fact]
    public async Task Concurrent_executors_keep_authorization_decisions_isolated()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddEditorServices();
        services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<AuthorizationTests.Protected>());
        var reader = new Mock<IUserPermissionReader>();
        reader.Setup(x => x.GetPermissionLevelAsync(11, PermissionFeature.Orders, It.IsAny<CancellationToken>())).ReturnsAsync(PermissionLevel.Write);
        reader.Setup(x => x.GetPermissionLevelAsync(22, PermissionFeature.Orders, It.IsAny<CancellationToken>())).ReturnsAsync(PermissionLevel.Read);
        services.AddScoped(_ => reader.Object);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var writer = new AuthenticatedRequestExecutor(provider.GetRequiredService<IServiceScopeFactory>(), User(11));
        var readerExecutor = new AuthenticatedRequestExecutor(provider.GetRequiredService<IServiceScopeFactory>(), User(22));
        var values = await Task.WhenAll(writer.ExecuteAsync(new CheckPermissionQuery(PermissionFeature.Orders, PermissionLevel.Write)),
            readerExecutor.ExecuteAsync(new CheckPermissionQuery(PermissionFeature.Orders, PermissionLevel.Write)));
        Assert.Equal(new[] { true, false }, values);
        await Assert.ThrowsAsync<AuthorizationDeniedException>(() => readerExecutor.ExecuteAsync(new AuthorizationTests.Protected()));
        Assert.Equal(7, await writer.ExecuteAsync(new AuthorizationTests.Protected()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Web_policies_delegate_to_Application_check_via_executor(bool allowed)
    {
        var executor = new Mock<IRequestExecutor>(MockBehavior.Strict);
        executor.Setup(x => x.ExecuteAsync(It.Is<CheckPermissionQuery>(query => query.Feature == PermissionFeature.Documents && query.RequiredLevel == PermissionLevel.Write), It.IsAny<CancellationToken>())).ReturnsAsync(allowed);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped(_ => executor.Object);
        services.AddPermissionAuthorization();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
        var policy = new PermissionAuthorizeAttribute(PermissionFeature.Documents, PermissionLevel.Write).Policy;
        Assert.Equal(allowed, (await authorization.AuthorizeAsync(Principal(11), null, policy)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, policy)).Succeeded);
        executor.Verify(x => x.ExecuteAsync(It.IsAny<CheckPermissionQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Every_file_action_has_typed_read_or_write_policy()
    {
        var controllers = new[] { typeof(OrderHub.Web.Controllers.Files.AttachmentsController),
            typeof(OrderHub.Web.Controllers.Files.InvoicesController), typeof(OrderHub.Web.Controllers.Files.QuotationsController),
            typeof(OrderHub.Web.Controllers.Files.ProformaInvoicesController) };
        foreach (var controller in controllers)
        foreach (var method in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            var level = method.IsDefined(typeof(HttpGetAttribute)) ? PermissionLevel.Read : PermissionLevel.Write;
            var feature = controller == controllers[0] ? PermissionFeature.Orders : PermissionFeature.Documents;
            Assert.Equal(new PermissionAuthorizeAttribute(feature, level).Policy, method.GetCustomAttribute<PermissionAuthorizeAttribute>().Policy);
        }
    }

    [Fact]
    public async Task Application_denial_maps_to_generic_HTTP_403()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        await new AuthorizationDeniedMiddleware(_ => throw new AuthorizationDeniedException()).InvokeAsync(context);
        Assert.Equal(403, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        Assert.Equal(new AuthorizationDeniedException().Message, await new StreamReader(context.Response.Body).ReadToEndAsync());
    }

    [Fact]
    public async Task HTTP_permission_policy_denial_returns_403_without_invoking_endpoint()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPermissionAuthorization();
        using var provider = services.BuildServiceProvider();
        var policy = await provider.GetRequiredService<IAuthorizationPolicyProvider>().GetPolicyAsync(
            new PermissionAuthorizeAttribute(PermissionFeature.Documents, PermissionLevel.Read).Policy);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var invoked = false;
        await provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>().HandleAsync(
            _ => { invoked = true; return Task.CompletedTask; }, context, policy,
            Microsoft.AspNetCore.Authorization.Policy.PolicyAuthorizationResult.Forbid());
        Assert.False(invoked);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task HTML_permission_denial_keeps_the_existing_access_denied_navigation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPermissionAuthorization();
        using var provider = services.BuildServiceProvider();
        var policy = await provider.GetRequiredService<IAuthorizationPolicyProvider>().GetPolicyAsync(
            new PermissionAuthorizeAttribute(PermissionFeature.Orders, PermissionLevel.Read).Policy);
        var context = new DefaultHttpContext();
        context.Request.Headers.Accept = "text/html";
        await provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>().HandleAsync(
            _ => throw new InvalidOperationException("Endpoint must not run"), context, policy,
            Microsoft.AspNetCore.Authorization.Policy.PolicyAuthorizationResult.Forbid());
        Assert.Equal(302, context.Response.StatusCode);
        Assert.Equal("/access-denied", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public void Business_routes_have_permissions_and_admin_routes_retain_Admin_gate()
    {
        var pages = typeof(PermissionAccess).Assembly.GetTypes().Where(type => type.Namespace?.Contains("Components.Pages.Features") == true
            && !type.Namespace.Contains("Authentication") && !type.Namespace.Contains("Setup")
            && type.GetCustomAttributes<Microsoft.AspNetCore.Components.RouteAttribute>().Any()).ToArray();
        Assert.NotEmpty(pages);
        foreach (var page in pages)
        {
            Assert.NotNull(page.GetCustomAttribute<PermissionAuthorizeAttribute>());
            if (page.Namespace.Contains("Administration"))
                Assert.Contains(page.GetCustomAttributes<AuthorizeAttribute>(), attribute => attribute.Roles == "Admin");
        }
    }

    public sealed class ProtectedHandler : IRequestHandler<AuthorizationTests.Protected, int>
    {
        public Task<int> Handle(AuthorizationTests.Protected request, CancellationToken cancellationToken) => Task.FromResult(7);
    }

    private static ClaimsPrincipal Principal(int id) => new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "Cookies"));
    private static AuthenticatedUser User(int id)
    {
        var provider = new Mock<AuthenticationStateProvider>();
        provider.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(Principal(id)));
        return new AuthenticatedUser(provider.Object, new HttpContextAccessor());
    }
}
