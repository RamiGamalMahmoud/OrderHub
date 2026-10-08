using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Setup.Suppliers;
using OrderHub.Application.Features.Setup.Deliverymen;
using OrderHub.Application.Features.Setup.ShippingCarriers;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;

namespace OrderHub.Tests.Infrastructure;

public class SupplierDeliverymanShippingCarrierPermissionsTests
{
    [Theory]
    [InlineData(PermissionFeature.Suppliers, PermissionLevel.Read)]
    [InlineData(PermissionFeature.Suppliers, PermissionLevel.Write)]
    [InlineData(PermissionFeature.Deliverymen, PermissionLevel.Read)]
    [InlineData(PermissionFeature.Deliverymen, PermissionLevel.Write)]
    [InlineData(PermissionFeature.ShippingCarriers, PermissionLevel.Read)]
    [InlineData(PermissionFeature.ShippingCarriers, PermissionLevel.Write)]
    public async Task Feature_permission_is_independent_and_editor_city_lookup_needs_no_Cities_grant(
        PermissionFeature feature,
        PermissionLevel level)
    {
        using var factory = TestDb.CreateFactory();
        int userId;
        int cityId;

        using (var db = factory.CreateDbContext())
        {
            var city = new City("City");
            var role = new Role("Scoped feature role");
            role.SetPermission(feature, level);
            var user = User.Create("scoped", "hash", "Scoped");
            user.AssignRole(role);
            db.Cities.Add(city);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            userId = user.Id;
            cityId = city.Id;
        }

        var services = new ServiceCollection();
        services.AddLogging();
        OrderHub.Application.ServiceCollectionExtensions.AddApplication(services);
        OrderHub.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure(services, "unused");
        services.RemoveAll<AppDbContextFactory>();
        services.AddSingleton<AppDbContextFactory>(factory);
        services.AddScoped<ICurrentUser>(_ =>
            OrderHub.Tests.Application.AuthorizationTests.Identity(true, userId, false));
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        (PermissionFeature Feature, object Read, object Cities, object Create, object Update, object Delete)[] requests =
        [
            (PermissionFeature.Suppliers, new GetAllSuppliersQuery(), new GetSupplierEditorCitiesQuery(),
                new CreateSupplierCommand(new("Supplier", new(9, 0), new(17, 0), "Street", cityId, "01012345678", "+20")),
                new UpdateSupplierCommand(new(int.MaxValue, "Supplier", new(9, 0), new(17, 0), "Street", cityId, "01012345678", "+20")),
                new DeleteSupplierCommand(int.MaxValue)),
            (PermissionFeature.Deliverymen, new GetAllDeliverymenListQuery(), new GetDeliverymanEditorCitiesQuery(),
                new CreateDeliverymanCommand(new("Courier", cityId, "123")),
                new UpdateDeliverymanCommand(int.MaxValue, new("Courier", cityId, "123")), new DeleteDeliverymanCommand(int.MaxValue)),
            (PermissionFeature.ShippingCarriers, new GetShippingCarriersQuery(), new GetShippingCarrierEditorCitiesQuery(),
                new CreateShippingCarrierCommand(new("Carrier", 10, "+20", "01012345678", cityId, "Street")),
                new UpdateShippingCarrierCommand(new(int.MaxValue, "Carrier", 10, "+20", "01012345678", cityId, "Street")),
                new DeleteShippingCarrierCommand(int.MaxValue))
        ];

        foreach (var request in requests)
        {
            if (request.Feature == feature)
            {
                await mediator.Send(request.Read);
            }
            else
            {
                await Assert.ThrowsAsync<AuthorizationDeniedException>(() => mediator.Send(request.Read));
            }

            foreach (object write in new[] { request.Cities, request.Create, request.Update, request.Delete })
            {
                if (request.Feature == feature && level == PermissionLevel.Write)
                {
                    await mediator.Send(write);
                }
                else
                {
                    await Assert.ThrowsAsync<AuthorizationDeniedException>(() => mediator.Send(write));
                }
            }
        }
    }
}
