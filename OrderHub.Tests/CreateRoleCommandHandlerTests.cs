using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Administration.Roles;
using OrderHub.Application.Features.Administration.Roles.Create;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Repositories;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Tests;

public sealed class CreateRoleCommandHandlerTests
{
    [Fact]
    public async Task CreateRoleCommandHandler_ReturnsNewRoleIdAfterSave()
    {
        PostgresTestDatabase.Reset();

        var options = PostgresTestDatabase.Options;

        using var context = new AppDbContext(options);

        var unitOfWork = new UnitOfWork(
            context,
            new NumberSequenceService(
                context,
                new AppDbContextFactory(options)));

        var handler = new CreateRoleCommandHandler(
            new RoleRepository(context),
            unitOfWork);

        var request = new CreateRoleRequest(
            "دور اختبار",
            [
                new RolePermissionInput(
                    PermissionFeature.Orders,
                    PermissionLevel.Read),

                new RolePermissionInput(
                    PermissionFeature.Users,
                    PermissionLevel.Write)
            ]);

        var result = await handler.Handle(
            new CreateRoleCommand(request),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value > 0);

        var saved = await context.Roles
            .Include(role => role.Permissions)
            .SingleAsync(role => role.Id == result.Value);

        Assert.Equal("دور اختبار", saved.Name);
        Assert.Equal(2, saved.Permissions.Count);
    }
}
