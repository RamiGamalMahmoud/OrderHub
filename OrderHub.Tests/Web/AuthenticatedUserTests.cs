using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces;
using OrderHub.Web.Services;

namespace OrderHub.Tests.Web;

public class AuthenticatedUserTests
{
    [Fact]
    public async Task Independent_circuits_resolve_their_own_cookie_identity_without_global_context()
    {
        var first = new AuthenticatedUser(Provider(11), new HttpContextAccessor());
        var second = new AuthenticatedUser(Provider(22), new HttpContextAccessor());
        var ids = await Task.WhenAll(first.GetUserIdAsync(), second.GetUserIdAsync());
        Assert.Equal(new int?[] { 11, 22 }, ids);
    }

    [Fact]
    public async Task Anonymous_user_has_no_effective_identity()
    {
        var provider = new Mock<AuthenticationStateProvider>();
        provider.Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        Assert.Null(await new AuthenticatedUser(provider.Object, new HttpContextAccessor()).GetUserIdAsync());
    }

    [Fact]
    public async Task Http_operations_use_request_principal_when_circuit_state_is_unavailable()
    {
        var provider = new Mock<AuthenticationStateProvider>();
        provider.Setup(x => x.GetAuthenticationStateAsync()).Throws(new InvalidOperationException());
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = Principal(33) } };
        Assert.Equal(33, await new AuthenticatedUser(provider.Object, accessor).GetUserIdAsync());
    }

    [Fact]
    public async Task Executor_transfers_identity_to_a_fresh_scope_and_keeps_operations_isolated()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEditorServices();
        services.AddSingleton(Provider(99));
        services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<ReadOperationIdentity>());
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var first = new AuthenticatedRequestExecutor(provider.GetRequiredService<IServiceScopeFactory>(), User(11));
        var second = new AuthenticatedRequestExecutor(provider.GetRequiredService<IServiceScopeFactory>(), User(22));
        var ids = await Task.WhenAll(first.ExecuteAsync(new ReadOperationIdentity()), second.ExecuteAsync(new ReadOperationIdentity()));
        Assert.Equal(new int?[] { 11, 22 }, ids);
        using var outsideOperation = provider.CreateScope();
        Assert.Equal(99, await outsideOperation.ServiceProvider.GetRequiredService<ICurrentUser>().GetUserIdAsync());
    }

    private static ICurrentUser User(int id)
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(x => x.GetUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(id);
        return user.Object;
    }

    private static AuthenticationStateProvider Provider(int id)
    {
        var provider = new Mock<AuthenticationStateProvider>();
        provider.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(Principal(id)));
        return provider.Object;
    }

    private static ClaimsPrincipal Principal(int id) => new(new ClaimsIdentity(
        new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) }, "Cookie"));

    public record ReadOperationIdentity : IRequest<int?>;
    public sealed class ReadOperationIdentityHandler(ICurrentUser user) : IRequestHandler<ReadOperationIdentity, int?>
    {
        public Task<int?> Handle(ReadOperationIdentity request, CancellationToken cancellationToken) =>
            user.GetUserIdAsync(cancellationToken);
    }
}
