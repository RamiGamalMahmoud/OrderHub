using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Administration.Roles.GetRole;
using OrderHub.Application.Features.Administration.Roles.GetRoles;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.Administration;
using OrderHub.Infrastructure.Features.Administration.Users;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class RoleReadServiceTests
{
    [Fact]
    public async Task RoleReadService_ProjectsSealFlagsAndPerLevelCounts()
    {
        using var db = new RbacTestDb();
        await db.SeedAsync();

        var customRole = new Role("مخصص");
        customRole.SetPermission(PermissionFeature.Orders, PermissionLevel.Read);
        customRole.SetPermission(PermissionFeature.Products, PermissionLevel.Write);
        customRole.SetPermission(PermissionFeature.Clients, PermissionLevel.Approve);

        db.Context.Roles.Add(customRole);
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        var service = new RoleReadService(
            new AppDbContextFactory(db.Options));

        IReadOnlyList<RoleListItem> roles = await service.GetAsync();

        Assert.Equal(6, roles.Count);
        Assert.All(roles.Take(5), role => Assert.True(role.IsSealed));
        Assert.Equal("مخصص", roles[5].Name);
        Assert.False(roles[5].IsSealed);

        RoleListItem manager = roles.Single(role => role.RoleKey == "manager");
        Assert.Equal(0, manager.ReadCount);
        Assert.Equal(1, manager.WriteCount);
        Assert.Equal(12, manager.ApproveCount);
        Assert.Equal(13, manager.PermissionCount);

        RoleListItem sales = roles.Single(role => role.RoleKey == "sales");
        Assert.Equal(8, sales.ReadCount);
        Assert.Equal(2, sales.WriteCount);
        Assert.Equal(0, sales.ApproveCount);
        Assert.Equal(10, sales.PermissionCount);

        RoleListItem custom = roles.Single(role => role.Name == "مخصص");
        Assert.Equal(1, custom.ReadCount);
        Assert.Equal(1, custom.WriteCount);
        Assert.Equal(1, custom.ApproveCount);
        Assert.Equal(3, custom.PermissionCount);
        Assert.Null(custom.RoleKey);

        GetRoleResponse details = await service.GetRoleDetailsAsync(manager.Id);

        Assert.NotNull(details);
        Assert.True(details.IsSealed);
        Assert.Equal("manager", details.RoleKey);
        Assert.Equal(13, details.Permissions.Count);
        Assert.Contains(
            details.Permissions,
            permission =>
                permission.Feature == PermissionFeature.Warehouses &&
                permission.Level == PermissionLevel.Write);
        Assert.Contains(
            details.Permissions,
            permission =>
                permission.Feature == PermissionFeature.Inventory &&
                permission.Level == PermissionLevel.Approve);
    }

    [Fact]
    public async Task UserReadService_ProjectsIsRoleSealed()
    {
        using var db = new RbacTestDb();
        await db.SeedAsync();

        Role managerRole = await db.Context.Roles
            .SingleAsync(role => role.RoleKey == "manager");

        var customRole = new Role("مخصص");
        db.Context.Roles.Add(customRole);

        User sealedRoleUser = User.Create("sealed", "hash", "مستخدم دور جاهز");
        sealedRoleUser.AssignRole(managerRole);

        User customRoleUser = User.Create("custom", "hash", "مستخدم دور مخصص");
        customRoleUser.AssignRole(customRole);

        User noRoleUser = User.Create("norole", "hash", "مستخدم بدون دور");

        db.Context.Users.AddRange(
            sealedRoleUser,
            customRoleUser,
            noRoleUser);

        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        var service = new UserReadService(
            new AppDbContextFactory(db.Options));

        var users = await service.GetAsync();

        Assert.True(users.Single(user => user.UserName == "sealed").IsRoleSealed);
        Assert.False(users.Single(user => user.UserName == "custom").IsRoleSealed);
        Assert.False(users.Single(user => user.UserName == "norole").IsRoleSealed);
        Assert.Null(users.Single(user => user.UserName == "norole").RoleId);
    }
}
