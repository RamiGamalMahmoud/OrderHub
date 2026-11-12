using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Authorization;
using OrderHub.Application.Features.Warehouses.GetKeeperCandidates;
using OrderHub.Domain.Authorization;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Reopsitories;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrderHub.Tests;

public sealed class WarehouseKeeperCandidateTests
{
    [Fact]
    public async Task Handler_ReturnsOnlyActiveUsersWhoCanWriteInventory()
    {
        using var db = new RbacTestDb();
        await db.SeedAsync();

        Role storekeeperRole = await db.Context.Roles
            .SingleAsync(role => role.RoleKey == SealedRolePresets.StorekeeperKey);
        Role salesRole = await db.Context.Roles
            .SingleAsync(role => role.RoleKey == SealedRolePresets.SalesKey);
        Role managerRole = await db.Context.Roles
            .SingleAsync(role => role.RoleKey == SealedRolePresets.ManagerKey);

        User admin = User.Create(
            "admin",
            "hash",
            "أمين النظام",
            isAdmin: true);

        User keeper = User.Create("keeper", "hash", "أحمد المخزن");
        keeper.AssignRole(storekeeperRole);

        User sales = User.Create("sales", "hash", "بائع");
        sales.AssignRole(salesRole);

        User manager = User.Create("manager", "hash", "مدير");
        manager.AssignRole(managerRole);

        User noRole = User.Create("norole", "hash", "بلا دور");

        User inactive = User.Create(
            "inactive",
            "hash",
            "موظف موقوف",
            isActive: false);
        inactive.AssignRole(storekeeperRole);

        db.Context.Users.AddRange(
            admin,
            keeper,
            sales,
            manager,
            noRole,
            inactive);

        await db.Context.SaveChangesAsync();
        db.Context.ChangeTracker.Clear();

        var handler = new GetWarehouseKeeperCandidatesQueryHandler(
            new UserRepository(db.Context));

        var candidates = await handler.Handle(
            new GetWarehouseKeeperCandidatesQuery(),
            CancellationToken.None);

        Assert.Equal(3, candidates.Count);
        Assert.Equal(
            new[] { admin, keeper, manager }
                .OrderBy(user => user.DisplayName, StringComparer.Ordinal)
                .Select(user => user.DisplayName),
            candidates.Select(candidate => candidate.DisplayName));

        Assert.DoesNotContain(candidates, candidate => candidate.DisplayName == "بائع");
        Assert.DoesNotContain(candidates, candidate => candidate.DisplayName == "بلا دور");
        Assert.DoesNotContain(candidates, candidate => candidate.DisplayName == "موظف موقوف");
    }

    [Fact]
    public void Query_RequiresWarehousesWrite()
    {
        RequirePermissionAttribute? attribute = typeof(GetWarehouseKeeperCandidatesQuery)
            .GetCustomAttribute<RequirePermissionAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(PermissionFeature.Warehouses, attribute.Feature);
        Assert.Equal(PermissionLevel.Write, attribute.RequiredLevel);
    }

    [Fact]
    public void Candidate_ExposesOnlyMinimalFields()
    {
        string[] properties = typeof(WarehouseKeeperCandidate)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["DisplayName", "Id"], properties);
    }
}
