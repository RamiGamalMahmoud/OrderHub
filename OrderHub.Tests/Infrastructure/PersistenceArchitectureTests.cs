using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.GetEditorLookups;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Products;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Application.Features.Setup.Cities;
using OrderHub.Application.Features.Setup.Clients;
using OrderHub.Application.Features.Setup.Deliverymen;
using OrderHub.Application.Features.Setup.PaymentMethods;
using OrderHub.Application.Features.Setup.ShippingCarriers;
using OrderHub.Application.Features.Setup.Suppliers;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;
using OrderHub.Web.Services;

namespace OrderHub.Tests.Infrastructure;

public class PersistenceArchitectureTests
{
    [Fact]
    public void Persistence_contracts_and_handlers_preserve_project_boundaries_and_unique_DI()
    {
        var application = typeof(IRequestExecutor).Assembly;
        var forbidden = new[] { "Microsoft.EntityFrameworkCore", "Npgsql", "OrderHub.Infrastructure", "OrderHub.Web" };
        Assert.DoesNotContain(application.GetReferencedAssemblies(), reference => forbidden.Any(prefix => reference.Name.StartsWith(prefix)));
        Assert.DoesNotContain(typeof(City).Assembly.GetReferencedAssemblies(), reference => forbidden.Any(prefix => reference.Name.StartsWith(prefix)));
        Assert.DoesNotContain(typeof(AppDbContext).Assembly.GetTypes(), type => type.GetInterfaces().Any(IsRequestHandler));

        var services = new ServiceCollection();
        services.AddApplication();
        OrderHub.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure(services, "unused");
        services.AddEditorServices();
        var executor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IRequestExecutor));
        Assert.Equal(typeof(AuthenticatedRequestExecutor), executor.ImplementationType);
        Assert.Equal(ServiceLifetime.Scoped, executor.Lifetime);
        var persistence = services.Where(descriptor => descriptor.ServiceType.Assembly == application
            && descriptor.ServiceType.IsInterface && descriptor.ServiceType.Name.EndsWith("Reader"));
        Assert.NotEmpty(persistence);
        foreach (var group in persistence.GroupBy(descriptor => descriptor.ServiceType))
        {
            var registration = Assert.Single(group);
            Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
            Assert.Equal(typeof(AppDbContext).Assembly, registration.ImplementationType.Assembly);
            Assert.DoesNotContain(registration.ServiceType.GetMethods(), method =>
                method.ReturnType.ToString().Contains("IQueryable") || method.ReturnType.ToString().Contains("DbContext"));
        }
        Assert.All(services.Where(descriptor => IsRequestHandler(descriptor.ServiceType)), descriptor =>
            Assert.Equal(application, descriptor.ImplementationType.Assembly));
    }

    [Fact]
    public async Task Reference_info_and_editor_lookup_paths_resolve_through_production_DI()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        Assert.Empty(await mediator.Send(new GetCitiesInfoQuery()));
        Assert.Empty(await mediator.Send(new GetCategoriesInfoQuery()));
        Assert.Empty(await mediator.Send(new GetRootCategoriesQuery()));
        Assert.Empty(await mediator.Send(new GetSubCategoriesQuery(1)));
        Assert.Empty(await mediator.Send(new GetClientsInfoQuery()));
        Assert.Empty(await mediator.Send(new GetSuppliersInfoQuery()));
        Assert.Empty(await mediator.Send(new GetAllDeliverymenInfoQuery()));
        Assert.Empty(await mediator.Send(new GetAllShippingCarriersInfoQuery()));
        Assert.Empty(await mediator.Send(new GetProductsInfoQuery()));
        var methods = (await mediator.Send(new GetPaymentMethodListQuery())).ToList();
        Assert.Equal(4, methods.Count);
        Assert.All(methods, method => Assert.True(method.IsActive));
        var lookups = scope.ServiceProvider.GetRequiredService<IOrderEditorLookupReader>();
        Assert.Empty(await lookups.GetClients());
        var paymentLookups = (await lookups.GetPaymentMethods()).ToList();
        Assert.Equal(methods.Select(method => method.Id), paymentLookups.Select(method => method.Id));
        Assert.All(paymentLookups, method => Assert.Equal(method.DisplayName, method.Description));
        Assert.Empty(await lookups.GetDeliveryMethods());
        Assert.Empty(await lookups.GetShippingCarriers());
        var editorLookups = await mediator.Send(new GetOrderEditorLookupsQuery());
        Assert.Empty(editorLookups.Clients);
        Assert.Equal(4, editorLookups.PaymentMethods.Count);
        Assert.Empty(editorLookups.Deliverymen);
        Assert.Empty(editorLookups.ShippingCarriers);
        Assert.Empty(scope.ServiceProvider.GetRequiredService<AppDbContext>().ChangeTracker.Entries());
    }

    private static bool IsRequestHandler(Type type) => type.IsGenericType
        && (type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) || type.GetGenericTypeDefinition() == typeof(IRequestHandler<>));
}
