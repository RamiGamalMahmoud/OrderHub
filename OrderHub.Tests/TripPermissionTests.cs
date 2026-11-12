using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;

namespace OrderHub.Tests;

public sealed class TripPermissionTests
{
    [Fact]
    public void PermissionFeature_Trips_ExistsWithArabicDescription()
    {
        Assert.True(Enum.IsDefined(typeof(PermissionFeature), PermissionFeature.Trips));
        
        var description = PermissionFeature.Trips
            .GetType()
            .GetField(PermissionFeature.Trips.ToString())
            ?.GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false)
            .Cast<System.ComponentModel.DescriptionAttribute>()
            .FirstOrDefault()
            ?.Description;
        
        Assert.Equal("المشاوير", description);
    }

    [Fact]
    public void SealedRoles_DoNotIncludeTrips()
    {
        foreach (var preset in OrderHub.Domain.Authorization.SealedRolePresets.All)
        {
            Assert.DoesNotContain(
                preset.Permissions,
                permission => permission.Feature == PermissionFeature.Trips);
        }
    }

    [Fact]
    public async Task SealedRoleSeed_TotalPermissionsUnchanged()
    {
        using var db = new RbacTestDb();
        await db.SeedAsync();

        var roles = await db.Context.Roles
            .Include(role => role.Permissions)
            .ToListAsync();

        Assert.Equal(5, roles.Count);
        Assert.Equal(38, roles.Sum(role => role.Permissions.Count));
    }
}
