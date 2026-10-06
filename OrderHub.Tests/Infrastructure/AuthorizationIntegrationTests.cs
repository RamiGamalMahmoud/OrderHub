using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Setup.Cities;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Infrastructure;

public class AuthorizationIntegrationTests
{
    [Fact]
    public async Task Real_pipeline_prevents_mutations_and_observes_committed_role_changes()
    {
        using var factory = TestDb.CreateFactory();
        int id;
        using (var db = factory.CreateDbContext())
        {
            var role = new Role("Sales");
            role.SetPermission(PermissionFeature.Cities, PermissionLevel.Read);
            var user = User.Create("test", "hash", "Test");
            user.AssignRole(role);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            id = user.Id;
        }
        // Use a real role-backed actor with the existing database fixture and production DI.
        var services = new ServiceCollection();
        services.AddLogging();
        OrderHub.Application.ServiceCollectionExtensions.AddApplication(services);
        OrderHub.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure(services, "unused");
        services.RemoveAll<OrderHub.Infrastructure.AppDbContextFactory>();
        services.AddSingleton<OrderHub.Infrastructure.AppDbContextFactory>(factory);
        services.AddScoped<ICurrentUser>(_ => OrderHub.Tests.Application.AuthorizationTests.Identity(true, id, false));
        using var normalProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var normalScope = normalProvider.CreateScope();
        var mediator = normalScope.ServiceProvider.GetRequiredService<IMediator>();
        Assert.Empty(await mediator.Send(new GetAllCitiesQuery()));
        await Assert.ThrowsAsync<AuthorizationDeniedException>(() => mediator.Send(new CreateCityCommand(new("Denied"))));
        using (var db = factory.CreateDbContext())
        {
            Assert.Empty(db.Cities);
            var role = db.Roles.Single();
            // Load the permission tracked for a normal Domain mutation.
            db.Entry(role).Collection(x => x.Permissions).Load();
            role.SetPermission(PermissionFeature.Cities, PermissionLevel.Write);
            await db.SaveChangesAsync();
        }
        Assert.True((await mediator.Send(new CreateCityCommand(new("Allowed")))).IsSuccess);
        using (var db = factory.CreateDbContext())
        {
            var role = db.Roles.Single();
            db.Entry(role).Collection(x => x.Permissions).Load();
            role.SetPermission(PermissionFeature.Cities, PermissionLevel.Read);
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<AuthorizationDeniedException>(() => mediator.Send(new CreateCityCommand(new("Denied again"))));
        Assert.Single(await mediator.Send(new GetAllCitiesQuery()));
    }

    [Fact]
    public void Every_business_request_declares_valid_metadata_with_only_explicit_exemptions()
    {
        var requests = typeof(CheckPermissionQuery).Assembly.GetTypes().Where(type => !type.IsAbstract &&
            type.GetInterfaces().Any(contract => contract == typeof(IRequest) ||
                contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>))).ToArray();
        var exempt = new[] { typeof(CheckPermissionQuery),
            typeof(OrderHub.Application.Features.Authentication.DatabaseLogin.DatabaseLoginCommand),
            typeof(OrderHub.Application.Features.Administration.Admin.CreateAdmin.CreateAdminCommand) };
        foreach (var request in requests.Except(exempt))
        {
            var permission = request.GetCustomAttribute<RequirePermissionAttribute>();
            Assert.True(permission is not null, request.FullName);
            Assert.True(Enum.IsDefined(permission.Feature), request.FullName);
            Assert.True(permission.RequiredLevel is PermissionLevel.Read or PermissionLevel.Write, request.FullName);
        }
        Assert.Equal(93, requests.Length - exempt.Length);
        Assert.All(exempt, request => Assert.Null(request.GetCustomAttribute<RequirePermissionAttribute>()));
    }
}
