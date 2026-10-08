using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Moq;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Domain.Enums;

namespace OrderHub.Tests.Application;

public class AuthorizationTests
{
    [Theory]
    [InlineData(PermissionLevel.None, PermissionLevel.Read, false)]
    [InlineData(PermissionLevel.Read, PermissionLevel.Read, true)]
    [InlineData(PermissionLevel.Write, PermissionLevel.Read, true)]
    [InlineData(PermissionLevel.None, PermissionLevel.Write, false)]
    [InlineData(PermissionLevel.Read, PermissionLevel.Write, false)]
    [InlineData(PermissionLevel.Write, PermissionLevel.Write, true)]
    [InlineData((PermissionLevel)99, PermissionLevel.Read, false)]
    public async Task Explicit_level_matrix(PermissionLevel actual, PermissionLevel required, bool allowed)
    {
        var reader = new Mock<IUserPermissionReader>();
        reader.Setup(x => x.GetPermissionLevelAsync(12, PermissionFeature.Orders, It.IsAny<CancellationToken>())).ReturnsAsync(actual);
        var service = new AuthorizationService(Identity(true, 12, false), reader.Object);
        Assert.Equal(allowed, await service.IsAllowedAsync(PermissionFeature.Orders, required));
        reader.Verify(x => x.GetPermissionLevelAsync(12, PermissionFeature.Orders, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false, 12, false)]
    [InlineData(false, 12, true)]
    [InlineData(true, null, true)]
    [InlineData(true, 0, true)]
    [InlineData(true, -1, false)]
    public async Task Invalid_identity_denies_without_database_read(bool authenticated, int? id, bool admin)
    {
        var reader = new Mock<IUserPermissionReader>(MockBehavior.Strict);
        Assert.False(await new AuthorizationService(Identity(authenticated, id, admin), reader.Object)
            .IsAllowedAsync(PermissionFeature.Orders, PermissionLevel.Read));
        reader.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(PermissionLevel.Read)]
    [InlineData(PermissionLevel.Write)]
    public async Task Admin_ignores_role_without_lookup(PermissionLevel level)
    {
        var reader = new Mock<IUserPermissionReader>(MockBehavior.Strict);
        Assert.True(await new AuthorizationService(Identity(true, 12, true), reader.Object).IsAllowedAsync(PermissionFeature.Users, level));
        reader.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(PermissionLevel.None)]
    [InlineData((PermissionLevel)99)]
    public async Task Invalid_requirement_fails_closed_even_for_admin(PermissionLevel required)
    {
        Assert.False(await new AuthorizationService(Identity(true, 12, true), new Mock<IUserPermissionReader>(MockBehavior.Strict).Object)
            .IsAllowedAsync(PermissionFeature.Orders, required));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Pipeline_denial_prevents_handler_side_effects(bool allowed)
    {
        var auth = new Mock<IAuthorizationService>();
        auth.Setup(x => x.IsAllowedAsync(PermissionFeature.Orders, PermissionLevel.Write, It.IsAny<CancellationToken>())).ReturnsAsync(allowed);
        var effects = 0;
        RequestHandlerDelegate<int> next = _ => Task.FromResult(++effects);
        var behavior = new AuthorizationBehavior<Protected, int>(auth.Object);
        if (allowed)
            Assert.Equal(1, await behavior.Handle(new Protected(), next, default));
        else
            await Assert.ThrowsAsync<AuthorizationDeniedException>(() => behavior.Handle(new Protected(), next, default));
        Assert.Equal(allowed ? 1 : 0, effects);
    }

    [Fact]
    public async Task Unprotected_request_continues_without_authorization_call()
    {
        var auth = new Mock<IAuthorizationService>(MockBehavior.Strict);
        Assert.Equal(42, await new AuthorizationBehavior<Unprotected, int>(auth.Object).Handle(new(), _ => Task.FromResult(42), default));
        auth.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Unknown_feature_fails_closed_without_lookup()
    {
        var reader = new Mock<IUserPermissionReader>(MockBehavior.Strict);
        Assert.False(await new AuthorizationService(Identity(true, 12, true), reader.Object)
            .IsAllowedAsync((PermissionFeature)999, PermissionLevel.Read));
        reader.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Void_request_denial_also_prevents_side_effects()
    {
        var auth = new Mock<IAuthorizationService>();
        var effects = 0;
        await Assert.ThrowsAsync<AuthorizationDeniedException>(() =>
            new AuthorizationBehavior<ProtectedVoid, Unit>(auth.Object).Handle(new(), _ =>
            {
                effects++;
                return Task.FromResult(Unit.Value);
            }, default));
        Assert.Equal(0, effects);
    }

    internal static ICurrentUser Identity(bool authenticated, int? id, bool admin)
    {
        var user = new Mock<ICurrentUser>();
        user.SetupGet(x => x.Id).Returns(id);
        user.SetupGet(x => x.IsAuthenticated).Returns(authenticated);
        user.SetupGet(x => x.IsAdmin).Returns(admin);
        user.SetupGet(x => x.UserName).Returns("operator");
        user.SetupGet(x => x.DisplayName).Returns("Operator");
        return user.Object;
    }

    [RequirePermission(PermissionFeature.Orders, PermissionLevel.Write)]
    public sealed record Protected : IRequest<int>;
    [RequirePermission(PermissionFeature.Properties, PermissionLevel.Write)]
    public sealed record ProtectedVoid : IRequest;
    public sealed record Unprotected : IRequest<int>;
}
