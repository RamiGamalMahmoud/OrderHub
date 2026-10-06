using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderHub.Application;
using OrderHub.Application.Features.Setup.Cities;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Application.Features.Setup.Categories.Contracts;
using OrderHub.Application.Features.Setup.Clients;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Deliverymen;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;
using OrderHub.Application.Features.Setup.ShippingCarriers;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.Suppliers;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Infrastructure;

namespace OrderHub.Tests.Infrastructure;

public class SetupPersistenceTests
{
    [Fact]
    public async Task City_commands_commit_domain_changes_and_read_projected_results()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = Services(factory);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        Assert.True((await mediator.Send(new CreateCityCommand(new("Cairo")))).IsSuccess);
        var city = Assert.Single(await mediator.Send(new GetAllCitiesQuery()));
        Assert.Equal("Cairo", city.Name);
        Assert.True((await mediator.Send(new UpdateCityCommand(new(city.Id, "Alexandria")))).IsSuccess);
        Assert.Equal("Alexandria", (await mediator.Send(new GetCityForEditQuery(city.Id))).Name);
        Assert.True((await mediator.Send(new DeleteCityCommand(city.Id))).IsSuccess);
        Assert.Empty(await mediator.Send(new GetAllCitiesQuery()));
        Assert.Equal("المدينة غير موجودة.", (await mediator.Send(new DeleteCityCommand(city.Id))).ErrorMessage);
    }

    [Theory]
    [InlineData("Client")]
    [InlineData("Deliveryman")]
    [InlineData("Supplier")]
    [InlineData("ShippingCarrier")]
    public async Task Reference_commands_share_the_operation_context_and_city_delete_guards_remain(string feature)
    {
        using var factory = TestDb.CreateFactory();
        using var provider = Services(factory);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new CreateCityCommand(new("City")));
        var city = Assert.Single(await mediator.Send(new GetAllCitiesQuery()));
        IRequest<Result> command = feature switch
        {
            "Client" => new CreateClientCommand(new("Client", "Street", city.Id, "01012345678", "+20", "Location")),
            "Deliveryman" => new CreateDeliverymanCommand(new("Deliveryman", city.Id, "01012345678")),
            "Supplier" => new CreateSupplierCommand(new("Supplier", new TimeOnly(9, 0), new TimeOnly(17, 0), "Street", city.Id, "01012345678", "+20")),
            _ => new CreateShippingCarrierCommand(new("Carrier", 10, "+20", "01012345678", city.Id, "Street"))
        };
        Assert.True((await mediator.Send(command)).IsSuccess);
        var failure = await mediator.Send(new DeleteCityCommand(city.Id));
        Assert.False(failure.IsSuccess);
        Assert.Equal(feature == "Deliveryman" ? "لا يمكن حذف المدينة لأنها مرتبطة بمناديب." : "لا يمكن حذف المدينة لأنها مرتبطة بعناوين.", failure.ErrorMessage);
        using var readScope = provider.CreateScope();
        var context = readScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await context.Cities.CountAsync());
        var count = feature switch
        {
            "Client" => await context.Clients.CountAsync(),
            "Deliveryman" => await context.Deliverymen.CountAsync(),
            "Supplier" => await context.Suppliers.CountAsync(),
            _ => await context.ShippingCarriers.CountAsync()
        };
        Assert.Equal(1, count);

        using var updateScope = provider.CreateScope();
        var updater = updateScope.ServiceProvider.GetRequiredService<IMediator>();
        IRequest<Result> update = feature switch
        {
            "Client" => new UpdateClientCommand(1, new("Updated", "New street", city.Id, "01098765432", "+20", "New location")),
            "Deliveryman" => new UpdateDeliverymanCommand(1, new("Updated", city.Id, "01098765432")),
            "Supplier" => new UpdateSupplierCommand(new(1, "Updated", new TimeOnly(10, 0), new TimeOnly(18, 0), "New street", city.Id, "01098765432", "+20")),
            _ => new UpdateShippingCarrierCommand(new(1, "Updated", 20, "+20", "01098765432", city.Id, "New street"))
        };
        var updateResult = await updater.Send(update);
        Assert.True(updateResult.IsSuccess, updateResult.ErrorMessage);
        var name = feature switch
        {
            "Client" => (await updater.Send(new GetClientEditQuery(1))).Name,
            "Deliveryman" => (await updater.Send(new GetDeliverymanForEditQuery(1))).Name,
            "Supplier" => (await updater.Send(new GetSupplierForEditQuery(1))).Name,
            _ => (await updater.Send(new GetShippingCarrierForEditQuery(1))).Name
        };
        Assert.Equal("Updated", name);
        IRequest<Result> delete = feature switch
        {
            "Client" => new DeleteClientCommand(1),
            "Deliveryman" => new DeleteDeliverymanCommand(1),
            "Supplier" => new DeleteSupplierCommand(1),
            _ => new DeleteShippingCarrierCommand(1)
        };
        Assert.True((await updater.Send(delete)).IsSuccess);
    }

    [Fact]
    public async Task Category_duplicate_check_tree_and_child_delete_guard_remain()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = Services(factory);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        Assert.True((await mediator.Send(new CreateCategoryCommand(new("Parent", null)))).IsSuccess);
        var parent = Assert.Single(await mediator.Send(new GetCategoryListQuery(null)));
        Assert.Equal("هذا القسم موجود بالفعل.", (await mediator.Send(new CreateCategoryCommand(new("Parent", null)))).ErrorMessage);
        Assert.True((await mediator.Send(new CreateCategoryCommand(new("Child", parent.Id)))).IsSuccess);
        var tree = Assert.Single(await mediator.Send(new GetCategoryTreeQuery()));
        Assert.Equal("Child", Assert.Single(tree.SubCategories).Name);
        Assert.Equal("لا يمكن حذف القسم لأنه يحتوي على أقسام فرعية.", (await mediator.Send(new DeleteCategoryCommand(parent.Id))).ErrorMessage);
        Assert.True((await mediator.Send(new UpdateCategoryCommand(new(parent.Id, "Renamed parent", null)))).IsSuccess);
        Assert.Equal("Renamed parent", (await mediator.Send(new GetCategoryForEditQuery(parent.Id))).Name);
    }

    [Fact]
    public async Task Read_cancellation_reaches_the_database()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = Services(factory);
        using var scope = provider.CreateScope();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scope.ServiceProvider.GetRequiredService<ICityReader>().GetAllCitiesAsync(cancellation.Token));
    }

    internal static ServiceProvider Services(TestDbFactory factory)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddScoped(_ => OrderHub.Tests.Application.AuthorizationTests.Identity(true, 1, true));
        OrderHub.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure(services, "unused");
        services.RemoveAll<AppDbContextFactory>();
        services.AddSingleton<AppDbContextFactory>(factory);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
