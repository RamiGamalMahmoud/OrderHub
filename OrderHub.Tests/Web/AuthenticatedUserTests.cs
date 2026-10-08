using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderHub.Application;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces;
using OrderHub.Web.Services;

namespace OrderHub.Tests.Web;

public class AuthenticatedUserTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Authenticated_identity_exposes_all_claim_values(bool isAdmin)
    {
        var operation = new OperationUser();
        operation.Initialize(await User(11, isAdmin).GetIdentityAsync());
        Assert.Equal(Expected(11, isAdmin), Read(operation));
    }

    [Fact]
    public async Task Anonymous_claims_do_not_become_an_effective_identity()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "11"), new Claim(ClaimTypes.Name, "ignored"),
            new Claim("DisplayName", "ignored"), new Claim(ClaimTypes.Role, "Admin")
        }));
        var operation = new OperationUser();
        operation.Initialize(await new AuthenticatedUser(Provider(principal), new HttpContextAccessor()).GetIdentityAsync());
        Assert.Equal(new CurrentUserIdentity(null, null, null, false, false), Read(operation));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData(null)]
    public async Task Invalid_user_identifier_does_not_manufacture_an_id(string identifier)
    {
        var claims = identifier is null ? Array.Empty<Claim>()
            : new[] { new Claim(ClaimTypes.NameIdentifier, identifier) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));
        var identity = await new AuthenticatedUser(Provider(principal), new HttpContextAccessor()).GetIdentityAsync();
        Assert.Null(identity.Id);
        Assert.True(identity.IsAuthenticated);
        Assert.False(identity.IsAdmin);
    }

    [Fact]
    public async Task Circuit_identity_takes_precedence_over_ambient_http_context()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = Principal(22) } };
        Assert.Equal(Expected(11, true), await new AuthenticatedUser(Provider(Principal(11, true)), accessor).GetIdentityAsync());
    }

    [Fact]
    public async Task Http_operations_use_request_principal_when_circuit_state_is_unavailable()
    {
        var provider = new Mock<AuthenticationStateProvider>();
        provider.Setup(x => x.GetAuthenticationStateAsync()).Throws(new InvalidOperationException());
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = Principal(33, true) } };
        Assert.Equal(Expected(33, true), await new AuthenticatedUser(provider.Object, accessor).GetIdentityAsync());
    }

    [Fact]
    public async Task Executor_transfers_complete_identity_and_keeps_concurrent_operations_isolated()
    {
        using var provider = Services().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var first = new AuthenticatedRequestExecutor(provider.GetRequiredService<IServiceScopeFactory>(), User(11, true));
        var second = new AuthenticatedRequestExecutor(provider.GetRequiredService<IServiceScopeFactory>(), User(22));
        var identities = await Task.WhenAll(first.ExecuteAsync(new ReadOperationIdentity()), second.ExecuteAsync(new ReadOperationIdentity()));
        Assert.Equal(new[] { Expected(11, true), Expected(22, false) }, identities);
        await Task.WhenAll(first.ExecuteAsync(new AssertOperationIdentity(Expected(11, true))),
            second.ExecuteAsync(new AssertOperationIdentity(Expected(22, false))));
        using var outsideOperation = provider.CreateScope();
        var currentUser = outsideOperation.ServiceProvider.GetRequiredService<ICurrentUser>();
        Assert.Same(outsideOperation.ServiceProvider.GetRequiredService<OperationUser>(), currentUser);
        Assert.Throws<InvalidOperationException>(() => currentUser.Id);
    }

    [Fact]
    public async Task Executor_preserves_anonymous_identity_in_the_fresh_scope()
    {
        using var provider = Services().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var anonymous = new AuthenticatedUser(Provider(new ClaimsPrincipal(new ClaimsIdentity())), new HttpContextAccessor());
        var executor = new AuthenticatedRequestExecutor(provider.GetRequiredService<IServiceScopeFactory>(), anonymous);
        Assert.Equal(new CurrentUserIdentity(null, null, null, false, false),
            await executor.ExecuteAsync(new ReadOperationIdentity()));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Initialized_identity_cannot_be_replaced_including_anonymous_identity(bool authenticated)
    {
        var identity = authenticated ? Expected(11, true) : new CurrentUserIdentity(null, null, null, false, false);
        var operation = new OperationUser();
        operation.Initialize(identity);
        Assert.True(operation.IsInitialized);
        Assert.Throws<InvalidOperationException>(() => operation.Initialize(Expected(22, false)));
        Assert.Equal(identity, Read(operation));
    }

    [Fact]
    public void Production_registration_has_one_scoped_web_executor()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        OrderHub.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure(
            services, "Host=localhost;Database=registration_test;Username=test;Password=test");
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IRequestExecutor));
        services.AddEditorServices();
        var executor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IRequestExecutor));
        Assert.Equal(ServiceLifetime.Scoped, executor.Lifetime);
        Assert.Equal(typeof(AuthenticatedRequestExecutor), executor.ImplementationType);
        Assert.All(services, descriptor =>
        {
            if (descriptor.ServiceType == typeof(ICurrentUser) || descriptor.ServiceType == typeof(OperationUser)
                || descriptor.ServiceType == typeof(AuthenticatedUser))
                Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        });
        services.AddLogging();
        services.AddScoped(_ => Provider(Principal(11, true)));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var resolved = first.ServiceProvider.GetRequiredService<IRequestExecutor>();
        Assert.IsType<AuthenticatedRequestExecutor>(resolved);
        Assert.Same(resolved, first.ServiceProvider.GetRequiredService<IRequestExecutor>());
        Assert.NotSame(resolved, second.ServiceProvider.GetRequiredService<IRequestExecutor>());
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IRequestExecutor>());
    }

    private static ServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEditorServices();
        services.AddScoped(_ => Provider(Principal(99, true)));
        services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<ReadOperationIdentity>());
        return services;
    }

    private static AuthenticatedUser User(int id, bool isAdmin = false) =>
        new(Provider(Principal(id, isAdmin)), new HttpContextAccessor());

    private static AuthenticationStateProvider Provider(ClaimsPrincipal principal)
    {
        var provider = new Mock<AuthenticationStateProvider>();
        provider.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(principal));
        return provider.Object;
    }

    private static ClaimsPrincipal Principal(int id, bool isAdmin = false)
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, id.ToString()), new Claim(ClaimTypes.Name, $"user{id}"),
            new Claim("DisplayName", $"User {id}")
        }, "Cookies");
        if (isAdmin)
            identity.AddClaim(new Claim(ClaimTypes.Role, "Admin"));
        return new ClaimsPrincipal(identity);
    }

    private static CurrentUserIdentity Expected(int id, bool isAdmin) =>
        new(id, $"user{id}", $"User {id}", true, isAdmin);

    private static CurrentUserIdentity Read(ICurrentUser user) =>
        new(user.Id, user.UserName, user.DisplayName, user.IsAuthenticated, user.IsAdmin);

    public record ReadOperationIdentity : IRequest<CurrentUserIdentity>;
    public sealed class ReadOperationIdentityHandler(ICurrentUser user)
        : IRequestHandler<ReadOperationIdentity, CurrentUserIdentity>
    {
        public async Task<CurrentUserIdentity> Handle(ReadOperationIdentity request, CancellationToken cancellationToken)
        {
            var before = Read(user);
            await Task.Yield();
            Assert.Equal(before, Read(user));
            return before;
        }
    }

    public record AssertOperationIdentity(CurrentUserIdentity Expected) : IRequest;
    public sealed class AssertOperationIdentityHandler(ICurrentUser user) : IRequestHandler<AssertOperationIdentity>
    {
        public Task Handle(AssertOperationIdentity request, CancellationToken cancellationToken)
        {
            Assert.Equal(request.Expected, Read(user));
            return Task.CompletedTask;
        }
    }
}
