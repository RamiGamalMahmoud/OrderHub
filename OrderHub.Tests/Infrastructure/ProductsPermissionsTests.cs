using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Products.Index;
using OrderHub.Application.Features.Products.Editor;
using OrderHub.Application.Features.Products.Create;
using OrderHub.Application.Features.Products.Update;
using OrderHub.Application.Features.Products.Delete;
using OrderHub.Application.Features.Setup.Deliverymen;
using OrderHub.Application.Features.Setup.ShippingCarriers;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;

namespace OrderHub.Tests.Infrastructure;

public class ProductsPermissionsTests
{
    [Theory]
    [InlineData(PermissionFeature.Products, PermissionLevel.Read)]
    [InlineData(PermissionFeature.Products, PermissionLevel.Write)]
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

        await mediator.Send(new GetProductIndexQuery());
        await Assert.ThrowsAsync<AuthorizationDeniedException>(() => mediator.Send(new OrderHub.Application.Features.Setup.Categories.GetCategoriesQuery()));
        await Assert.ThrowsAsync<AuthorizationDeniedException>(() => mediator.Send(new OrderHub.Application.Features.Setup.Suppliers.GetAllSuppliersQuery()));
        await Assert.ThrowsAsync<AuthorizationDeniedException>(() => mediator.Send(new OrderHub.Application.Features.Setup.Properties.GetAll.GetPropertiesQuery()));
        object[] writes =
        [
            new GetProductEditorQuery(),
            new GetProductEditorCategoriesQuery(),
            new CreateProductCommand(new("Product", "ABC", 10, int.MaxValue, [], [])),
            new UpdateProductCommand(int.MaxValue, new("Product", "ABC", 10, int.MaxValue, [], [])),
            new DeleteProductCommand(int.MaxValue)
        ];
        foreach (var request in writes)
        {
            if (level == PermissionLevel.Write)
            {
                await mediator.Send(request);
            }
            else
            {
                await Assert.ThrowsAsync<AuthorizationDeniedException>(() => mediator.Send(request));
            }
        }
    }
}
