using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Features.Administration;

namespace OrderHub.Tests.Infrastructure;

public class UserPermissionReaderTests
{
    [Theory]
    [InlineData(null, PermissionLevel.None)]
    [InlineData(PermissionLevel.None, PermissionLevel.None)]
    [InlineData(PermissionLevel.Read, PermissionLevel.Read)]
    [InlineData(PermissionLevel.Write, PermissionLevel.Write)]
    public async Task Reads_current_role_permission(PermissionLevel? stored, PermissionLevel expected)
    {
        using var factory = TestDb.CreateFactory();
        using var db = factory.CreateDbContext();
        var user = User.Create("reader", "hash", "Reader");
        if (stored.HasValue)
        {
            var role = new Role("Descriptive name only");
            role.SetPermission(PermissionFeature.Orders, stored.Value);
            role.SetPermission(PermissionFeature.Products, PermissionLevel.Write);
            user.AssignRole(role);
        }
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var reader = new UserPermissionReader(factory);
        Assert.Equal(expected, await reader.GetPermissionLevelAsync(user.Id, PermissionFeature.Orders));
        Assert.Equal(PermissionLevel.None, await reader.GetPermissionLevelAsync(user.Id, PermissionFeature.Users));
        Assert.Equal(PermissionLevel.None, await reader.GetPermissionLevelAsync(int.MaxValue, PermissionFeature.Orders));
    }

    [Fact]
    public async Task Next_operation_observes_permission_change_removal_and_role_reassignment()
    {
        using var factory = TestDb.CreateFactory();
        using var db = factory.CreateDbContext();
        var role = new Role("Sales");
        role.SetPermission(PermissionFeature.Orders, PermissionLevel.Write);
        var user = User.Create("sales", "hash", "Sales", isActive: false);
        user.AssignRole(role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var reader = new UserPermissionReader(factory);
        Assert.Equal(PermissionLevel.Write, await reader.GetPermissionLevelAsync(user.Id, PermissionFeature.Orders));
        role.SetPermission(PermissionFeature.Orders, PermissionLevel.Read);
        await db.SaveChangesAsync();
        Assert.Equal(PermissionLevel.Read, await reader.GetPermissionLevelAsync(user.Id, PermissionFeature.Orders));
        role.SetPermission(PermissionFeature.Orders, PermissionLevel.None);
        await db.SaveChangesAsync();
        Assert.Equal(PermissionLevel.None, await reader.GetPermissionLevelAsync(user.Id, PermissionFeature.Orders));
        var replacement = new Role("Other");
        replacement.SetPermission(PermissionFeature.Orders, PermissionLevel.Write);
        user.AssignRole(replacement);
        await db.SaveChangesAsync();
        Assert.Equal(PermissionLevel.Write, await reader.GetPermissionLevelAsync(user.Id, PermissionFeature.Orders));
        user.RemoveRole();
        await db.SaveChangesAsync();
        Assert.Equal(PermissionLevel.None, await reader.GetPermissionLevelAsync(user.Id, PermissionFeature.Orders));
    }
}
