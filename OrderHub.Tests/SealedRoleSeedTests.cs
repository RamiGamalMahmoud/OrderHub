using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Authorization;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class SealedRoleSeedTests
{
    [Fact]
    public async Task Seed_OnEmptyDatabase_CreatesFourSealedRoles()
    {
        using var db = new RbacTestDb();

        await db.SeedAsync();

        var roles = await db.Context.Roles
            .Include(role => role.Permissions)
            .ToListAsync();

        Assert.Equal(4, roles.Count);
        Assert.All(roles, role => Assert.True(role.IsSealed));
        Assert.All(roles, role => Assert.False(string.IsNullOrWhiteSpace(role.RoleKey)));
        Assert.Equal(4, roles.Select(role => role.RoleKey).Distinct().Count());
        Assert.Equal(31, roles.Sum(role => role.Permissions.Count));
        Assert.Equal(
            SealedRolePresets.All.Select(preset => preset.Name).OrderBy(name => name),
            roles.Select(role => role.Name).OrderBy(name => name));
    }

    [Fact]
    public async Task Seed_IsIdempotent()
    {
        using var db = new RbacTestDb();

        await db.SeedAsync();
        await db.SeedAsync();

        Assert.Equal(4, await db.Context.Roles.CountAsync());
        Assert.Equal(31, await db.Context.Set<RolePermission>().CountAsync());
    }

    [Fact]
    public async Task Seed_SkipsPreset_WhenNameCollides()
    {
        using var db = new RbacTestDb();

        var customRole = new Role("مبيعات");
        customRole.SetPermission(
            PermissionFeature.Products,
            PermissionLevel.Read);

        db.Context.Roles.Add(customRole);
        await db.Context.SaveChangesAsync();

        await db.SeedAsync();
        db.Context.ChangeTracker.Clear();

        Role reloaded = await db.Context.Roles
            .Include(role => role.Permissions)
            .SingleAsync(role => role.Name == "مبيعات");

        Assert.False(reloaded.IsSealed);
        Assert.Null(reloaded.RoleKey);

        RolePermission permission = Assert.Single(reloaded.Permissions);
        Assert.Equal(PermissionFeature.Products, permission.Feature);
        Assert.Equal(PermissionLevel.Read, permission.Level);

        Assert.Equal(4, await db.Context.Roles.CountAsync());
        Assert.False(await db.Context.Roles
            .AnyAsync(role => role.RoleKey == SealedRolePresets.SalesKey));
    }

    [Fact]
    public async Task Seed_ReAddsMissingPermissionCell()
    {
        using var db = new RbacTestDb();
        await db.SeedAsync();

        Role manager = await db.Context.Roles
            .SingleAsync(role => role.RoleKey == SealedRolePresets.ManagerKey);

        RolePermission removed = await db.Context.Set<RolePermission>()
            .SingleAsync(permission =>
                permission.RoleId == manager.Id &&
                permission.Feature == PermissionFeature.Clients);

        db.Context.Set<RolePermission>().Remove(removed);
        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        await db.SeedAsync();
        db.Context.ChangeTracker.Clear();

        var permissions = await db.Context.Set<RolePermission>()
            .Where(permission => permission.RoleId == manager.Id)
            .ToListAsync();

        Assert.Equal(11, permissions.Count);
        Assert.Contains(
            permissions,
            permission =>
                permission.Feature == PermissionFeature.Clients &&
                permission.Level == PermissionLevel.Approve);

        Assert.Equal(31, await db.Context.Set<RolePermission>().CountAsync());
    }

    [Fact]
    public async Task Seed_MatchesCatalog()
    {
        using var db = new RbacTestDb();
        await db.SeedAsync();

        var roles = await db.Context.Roles
            .Include(role => role.Permissions)
            .ToListAsync();

        Assert.Equal(SealedRolePresets.All.Count, roles.Count);

        foreach (SealedRolePreset preset in SealedRolePresets.All)
        {
            Role role = roles.Single(candidate => candidate.RoleKey == preset.RoleKey);

            Assert.True(role.IsSealed);
            Assert.Equal(preset.Name, role.Name);

            var expected = preset.Permissions
                .OrderBy(cell => cell.Feature)
                .ThenBy(cell => cell.Level)
                .ToList();

            var actual = role.Permissions
                .Select(permission => new RolePermissionPreset(
                    permission.Feature,
                    permission.Level))
                .OrderBy(cell => cell.Feature)
                .ThenBy(cell => cell.Level)
                .ToList();

            Assert.Equal(expected, actual);
        }
    }
}
