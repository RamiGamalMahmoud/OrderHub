using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Administration.Users.Update;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Authorization;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class UserRolePreservationTests
{
    [Fact]
    public async Task UpdateUser_PromoteToHigherManager_KeepsAssignedRole()
    {
        using var db = new RbacTestDb();
        await db.SeedAsync();

        Role managerRole = await db.Context.Roles
            .SingleAsync(role => role.RoleKey == SealedRolePresets.ManagerKey);

        User user = User.Create(
            "hm",
            "hash",
            "مدير أعلى",
            isActive: true,
            isAdmin: true);

        user.AssignRole(managerRole);
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();

        int userId = user.Id;
        db.Context.ChangeTracker.Clear();

        User loaded = await db.Context.Users
            .Include(candidate => candidate.Role)
            .SingleAsync(candidate => candidate.Id == userId);

        Assert.Equal(managerRole.Id, loaded.RoleId);

        var handler = new UpdateUserCommandHandler(
            new FakeUserRepository(loaded, hasOtherActiveAdmin: true),
            new RoleRepository(db.Context),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new UpdateUserCommand(
                userId,
                new UpdateUserRequest(
                    "hm",
                    "مدير أعلى",
                    IsActive: true,
                    IsAdmin: true,
                    RoleId: managerRole.Id)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(loaded.IsAdmin);
        Assert.Equal(managerRole.Id, loaded.RoleId);
        Assert.NotNull(loaded.Role);
        Assert.Equal(managerRole.Id, loaded.Role.Id);
        Assert.True(loaded.Role.IsSealed);
    }

    [Fact]
    public async Task UpdateUser_LastActiveHigherManager_GuardStillBlocks()
    {
        using var db = new RbacTestDb();
        await db.SeedAsync();

        Role managerRole = await db.Context.Roles
            .SingleAsync(role => role.RoleKey == SealedRolePresets.ManagerKey);

        User user = User.Create(
            "hm",
            "hash",
            "مدير أعلى",
            isActive: true,
            isAdmin: true);

        user.AssignRole(managerRole);
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();

        int userId = user.Id;
        db.Context.ChangeTracker.Clear();

        User loaded = await db.Context.Users
            .Include(candidate => candidate.Role)
            .SingleAsync(candidate => candidate.Id == userId);

        var handler = new UpdateUserCommandHandler(
            new FakeUserRepository(loaded, hasOtherActiveAdmin: false),
            new RoleRepository(db.Context),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new UpdateUserCommand(
                userId,
                new UpdateUserRequest(
                    "hm",
                    "مدير أعلى",
                    IsActive: false,
                    IsAdmin: false,
                    RoleId: managerRole.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "لا يمكن إلغاء صلاحية المدير أو تعطيل الحساب لأنه المدير النشط الوحيد في النظام.",
            result.ErrorMessage);

        Assert.True(loaded.IsAdmin);
        Assert.True(loaded.IsActive);
        Assert.Equal(managerRole.Id, loaded.RoleId);
    }

    [Fact]
    public async Task UpdateUser_DemoteWhenAnotherActiveHigherManagerExists_Succeeds()
    {
        using var db = new RbacTestDb();
        await db.SeedAsync();

        Role managerRole = await db.Context.Roles
            .SingleAsync(role => role.RoleKey == SealedRolePresets.ManagerKey);

        User user = User.Create(
            "hm",
            "hash",
            "مدير أعلى",
            isActive: true,
            isAdmin: true);

        user.AssignRole(managerRole);
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();

        int userId = user.Id;
        db.Context.ChangeTracker.Clear();

        User loaded = await db.Context.Users
            .Include(candidate => candidate.Role)
            .SingleAsync(candidate => candidate.Id == userId);

        var handler = new UpdateUserCommandHandler(
            new FakeUserRepository(loaded, hasOtherActiveAdmin: true),
            new RoleRepository(db.Context),
            new FakeUnitOfWork());

        var result = await handler.Handle(
            new UpdateUserCommand(
                userId,
                new UpdateUserRequest(
                    "hm",
                    "مدير أعلى",
                    IsActive: true,
                    IsAdmin: false,
                    RoleId: managerRole.Id)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(loaded.IsAdmin);
        Assert.Equal(managerRole.Id, loaded.RoleId);
        Assert.NotNull(loaded.Role);
    }

    private sealed class FakeUserRepository(
        User user,
        bool hasOtherActiveAdmin)
        : IUserRepository
    {
        public Task<User> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(user);

        public Task<User> GetWithRolePermissionsAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(user);

        public Task<User> GetByUsernameAsync(
            string username,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(user);

        public Task<bool> UsernameExistsAsync(
            string username,
            int? excludeUserId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> AnyAdminAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(hasOtherActiveAdmin);

        public Task<bool> AnyOtherActiveAdminAsync(
            int userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(hasOtherActiveAdmin);

        public Task<IReadOnlyList<User>> GetAllWithRolePermissionsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<User>>([user]);

        public void Add(User addedUser)
        {
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task BeginTransactionAsync(
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task CommitTransactionAsync(
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RollbackTransactionAsync(
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
