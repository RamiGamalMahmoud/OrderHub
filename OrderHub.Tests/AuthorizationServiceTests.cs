using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class AuthorizationServiceTests
{
    [Theory]
    [InlineData(PermissionLevel.Read, true)]
    [InlineData(PermissionLevel.Write, true)]
    [InlineData(PermissionLevel.Approve, true)]
    [InlineData(PermissionLevel.None, false)]
    public async Task Authorization_Read_IsSatisfiedBy_ReadWriteApprove(
        PermissionLevel actual,
        bool expected)
    {
        AuthorizationService service = CreateService(actual);

        bool allowed = await service.IsAllowedAsync(
            PermissionFeature.Orders,
            PermissionLevel.Read);

        Assert.Equal(expected, allowed);
    }

    [Theory]
    [InlineData(PermissionLevel.Read, false)]
    [InlineData(PermissionLevel.Write, true)]
    [InlineData(PermissionLevel.Approve, true)]
    [InlineData(PermissionLevel.None, false)]
    public async Task Authorization_Write_IsSatisfiedBy_WriteApprove(
        PermissionLevel actual,
        bool expected)
    {
        AuthorizationService service = CreateService(actual);

        bool allowed = await service.IsAllowedAsync(
            PermissionFeature.Orders,
            PermissionLevel.Write);

        Assert.Equal(expected, allowed);
    }

    [Theory]
    [InlineData(PermissionLevel.Read, false)]
    [InlineData(PermissionLevel.Write, false)]
    [InlineData(PermissionLevel.Approve, true)]
    [InlineData(PermissionLevel.None, false)]
    public async Task Authorization_Approve_IsSatisfiedOnlyByApprove(
        PermissionLevel actual,
        bool expected)
    {
        AuthorizationService service = CreateService(actual);

        bool allowed = await service.IsAllowedAsync(
            PermissionFeature.Orders,
            PermissionLevel.Approve);

        Assert.Equal(expected, allowed);
    }

    [Fact]
    public async Task Authorization_NoneRequirement_DeniesEvenForAdmin()
    {
        var reader = new FakeUserPermissionReader();
        var service = new AuthorizationService(
            new FakeCurrentUser { Id = 5, IsAdmin = true },
            reader);

        bool allowed = await service.IsAllowedAsync(
            PermissionFeature.Orders,
            PermissionLevel.None);

        Assert.False(allowed);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Authorization_UndefinedFeature_Denies()
    {
        var reader = new FakeUserPermissionReader();
        var service = new AuthorizationService(
            new FakeCurrentUser { Id = 5, IsAdmin = true },
            reader);

        bool allowed = await service.IsAllowedAsync(
            (PermissionFeature)999,
            PermissionLevel.Read);

        Assert.False(allowed);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Authorization_Unauthenticated_Denies()
    {
        AuthorizationService service = CreateService(
            PermissionLevel.Approve,
            user => user.IsAuthenticated = false);

        Assert.False(await service.IsAllowedAsync(
            PermissionFeature.Orders,
            PermissionLevel.Read));
    }

    [Fact]
    public async Task Authorization_MissingUserId_Denies()
    {
        AuthorizationService service = CreateService(
            PermissionLevel.Approve,
            user => user.Id = null);

        Assert.False(await service.IsAllowedAsync(
            PermissionFeature.Orders,
            PermissionLevel.Read));
    }

    [Fact]
    public async Task Authorization_Admin_BypassesWithoutReaderCall()
    {
        var reader = new ThrowingUserPermissionReader();
        var service = new AuthorizationService(
            new FakeCurrentUser { Id = 5, IsAdmin = true },
            reader);

        bool allowed = await service.IsAllowedAsync(
            PermissionFeature.Orders,
            PermissionLevel.Write);

        Assert.True(allowed);
        Assert.Equal(0, reader.Calls);
    }

    [Fact]
    public async Task Authorization_HigherManager_RequiresAuthenticatedAdminWithId()
    {
        Assert.True(await new AuthorizationService(
            new FakeCurrentUser { Id = 5, IsAdmin = true },
            new FakeUserPermissionReader()).IsHigherManagerAsync());

        Assert.False(await new AuthorizationService(
            new FakeCurrentUser { Id = 5, IsAdmin = false },
            new FakeUserPermissionReader()).IsHigherManagerAsync());

        Assert.False(await new AuthorizationService(
            new FakeCurrentUser { Id = null, IsAdmin = true },
            new FakeUserPermissionReader()).IsHigherManagerAsync());

        Assert.False(await new AuthorizationService(
            new FakeCurrentUser { Id = 5, IsAdmin = true, IsAuthenticated = false },
            new FakeUserPermissionReader()).IsHigherManagerAsync());
    }

    private static AuthorizationService CreateService(
        PermissionLevel actual,
        Action<FakeCurrentUser>? configure = null)
    {
        var user = new FakeCurrentUser { Id = 5 };
        configure?.Invoke(user);

        var reader = new FakeUserPermissionReader(
            new Dictionary<PermissionFeature, PermissionLevel>
            {
                [PermissionFeature.Orders] = actual
            });

        return new AuthorizationService(user, reader);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public int? Id { get; set; }

        public string? UserName { get; set; } = "user";

        public string? DisplayName { get; set; } = "User";

        public bool IsAuthenticated { get; set; } = true;

        public bool IsAdmin { get; set; }
    }

    private sealed class FakeUserPermissionReader(
        Dictionary<PermissionFeature, PermissionLevel>? levels = null)
        : IUserPermissionReader
    {
        private readonly Dictionary<PermissionFeature, PermissionLevel> _levels = levels ?? [];

        public int Calls { get; private set; }

        public Task<PermissionLevel> GetPermissionLevelAsync(
            int userId,
            PermissionFeature feature,
            CancellationToken cancellationToken = default)
        {
            Calls++;

            return Task.FromResult(
                _levels.GetValueOrDefault(
                    feature,
                    PermissionLevel.None));
        }
    }

    private sealed class ThrowingUserPermissionReader : IUserPermissionReader
    {
        public int Calls { get; private set; }

        public Task<PermissionLevel> GetPermissionLevelAsync(
            int userId,
            PermissionFeature feature,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            throw new InvalidOperationException(
                "The permission reader must not be called for a higher manager.");
        }
    }
}
