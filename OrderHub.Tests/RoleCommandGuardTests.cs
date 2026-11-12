using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Administration.Roles;
using OrderHub.Application.Features.Administration.Roles.Create;
using OrderHub.Application.Features.Administration.Roles.Delete;
using OrderHub.Application.Features.Administration.Roles.Update;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Repositories;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Tests;

public sealed class RoleCommandGuardTests
{
    [Theory]
    [InlineData("مبيعات")]
    [InlineData("مشتريات")]
    [InlineData("إدخال البيانات")]
    [InlineData("مدير")]
    [InlineData("  مبيعات  ")]
    public async Task CreateRole_ReservedPresetName_ReturnsArabicFailure(
        string name)
    {
        using var db = new RbacTestDb();

        var handler = new CreateRoleCommandHandler(
            new RoleRepository(db.Context),
            db.UnitOfWork);

        var result = await handler.Handle(
            new CreateRoleCommand(
                new CreateRoleRequest(name, [])),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("محجوز", result.ErrorMessage);
        Assert.Empty(await db.Context.Roles.ToListAsync());
    }

    [Fact]
    public async Task UpdateRole_Sealed_ReturnsArabicFailure()
    {
        using var db = new RbacTestDb();
        await db.SeedAsync();

        Role sales = await db.Context.Roles
            .SingleAsync(role => role.RoleKey == "sales");

        var handler = new UpdateRoleCommandHandler(
            new RoleRepository(db.Context),
            db.UnitOfWork);

        var result = await handler.Handle(
            new UpdateRoleCommand(
                sales.Id,
                new UpdateRoleRequest("مبيعات معدّلة", [])),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("دور جاهز", result.ErrorMessage);

        db.Context.ChangeTracker.Clear();

        Role reloaded = await db.Context.Roles
            .Include(role => role.Permissions)
            .SingleAsync(role => role.Id == sales.Id);

        Assert.Equal("مبيعات", reloaded.Name);
        Assert.Equal(10, reloaded.Permissions.Count);
    }

    [Fact]
    public async Task UpdateRole_Custom_RenameToReservedPresetName_ReturnsArabicFailure()
    {
        using var db = new RbacTestDb();

        var createHandler = new CreateRoleCommandHandler(
            new RoleRepository(db.Context),
            db.UnitOfWork);

        var created = await createHandler.Handle(
            new CreateRoleCommand(
                new CreateRoleRequest("دور مخصص", [])),
            CancellationToken.None);

        Assert.True(created.IsSuccess);

        var updateHandler = new UpdateRoleCommandHandler(
            new RoleRepository(db.Context),
            db.UnitOfWork);

        var result = await updateHandler.Handle(
            new UpdateRoleCommand(
                created.Value,
                new UpdateRoleRequest("  مدير  ", [])),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("محجوز", result.ErrorMessage);

        db.Context.ChangeTracker.Clear();

        Role reloaded = await db.Context.Roles
            .SingleAsync(role => role.Id == created.Value);

        Assert.Equal("دور مخصص", reloaded.Name);
    }

    [Fact]
    public async Task DeleteRole_Sealed_ReturnsArabicFailure()
    {
        using var db = new RbacTestDb();
        await db.SeedAsync();

        Role manager = await db.Context.Roles
            .SingleAsync(role => role.RoleKey == "manager");

        var handler = new DeleteRoleCommandHandler(
            new RoleRepository(db.Context),
            db.UnitOfWork);

        var result = await handler.Handle(
            new DeleteRoleCommand(manager.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("دور جاهز", result.ErrorMessage);

        db.Context.ChangeTracker.Clear();

        Assert.True(await db.Context.Roles
            .AnyAsync(role => role.Id == manager.Id));
    }

    [Fact]
    public async Task UpdateRole_ApproveLevel_Persists()
    {
        using var db = new RbacTestDb();

        var createHandler = new CreateRoleCommandHandler(
            new RoleRepository(db.Context),
            db.UnitOfWork);

        var created = await createHandler.Handle(
            new CreateRoleCommand(
                new CreateRoleRequest(
                    "مدير فرع",
                    [
                        new RolePermissionInput(
                            PermissionFeature.Orders,
                            PermissionLevel.Read)
                    ])),
            CancellationToken.None);

        Assert.True(created.IsSuccess);

        var updateHandler = new UpdateRoleCommandHandler(
            new RoleRepository(db.Context),
            db.UnitOfWork);

        var result = await updateHandler.Handle(
            new UpdateRoleCommand(
                created.Value,
                new UpdateRoleRequest(
                    "مدير فرع",
                    [
                        new RolePermissionInput(
                            PermissionFeature.Orders,
                            PermissionLevel.Approve)
                    ])),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        db.Context.ChangeTracker.Clear();

        Role saved = await db.Context.Roles
            .Include(role => role.Permissions)
            .SingleAsync(role => role.Id == created.Value);

        RolePermission permission = Assert.Single(saved.Permissions);
        Assert.Equal(PermissionFeature.Orders, permission.Feature);
        Assert.Equal(PermissionLevel.Approve, permission.Level);
    }
}
