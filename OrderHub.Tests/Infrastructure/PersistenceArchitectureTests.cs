using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.GetEditorLookups;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Products;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.Administration.Users.GetUser;
using OrderHub.Application.Features.Administration.Users.GetUsers;
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
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OrderHub.Tests.Infrastructure;

public class PersistenceArchitectureTests
{
    [Fact]
    public void Persistence_contracts_and_handlers_preserve_project_boundaries_and_unique_DI()
    {
        var application = typeof(IRequestExecutor).Assembly;
        var forbidden = new[] { "Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore", "OrderHub.Infrastructure", "OrderHub.Web" };
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
        using var factory = TestDb.CreateFactory();
        services.RemoveAll<AppDbContextFactory>();
        services.AddSingleton<AppDbContextFactory>(factory);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var contracts = application.GetTypes().Where(IsPersistenceContract).ToArray();
        Assert.NotEmpty(contracts);
        foreach (var contract in contracts)
        {
            var registration = Assert.Single(services, descriptor => descriptor.ServiceType == contract);
            var expectedLifetime = contract == typeof(IDraftReadService) ? ServiceLifetime.Singleton
                : contract == typeof(IUserDetailsService) || contract == typeof(IUserListingService) ? ServiceLifetime.Transient
                : ServiceLifetime.Scoped;
            Assert.Equal(expectedLifetime, registration.Lifetime);
            var implementation = scope.ServiceProvider.GetRequiredService(contract);
            Assert.Equal(typeof(AppDbContext).Assembly, implementation.GetType().Assembly);
            Assert.DoesNotContain(contract.GetMethods(), method =>
                method.ReturnType.ToString().Contains("IQueryable") || method.ReturnType.ToString().Contains("DbContext"));
        }
        Assert.All(services.Where(descriptor => IsRequestHandler(descriptor.ServiceType)), descriptor =>
            Assert.Equal(application, descriptor.ImplementationType.Assembly));
    }

    [Fact]
    public void Obsolete_persistence_types_are_absent_and_Infrastructure_does_not_register_MediatR()
    {
        var application = typeof(IRequestExecutor).Assembly;
        var infrastructure = typeof(AppDbContext).Assembly;
        foreach (var name in new[]
        {
            "OrderHub.Application.Interfaces.Repositories.ISupplierRepository",
            "OrderHub.Application.Interfaces.Repositories.IDeliverymanRepository",
            "OrderHub.Application.Features.Products.Contracts.IProductStore",
            "OrderHub.Application.Interfaces.Repositories.IPropertyStore",
            "OrderHub.Application.Interfaces.ILookupService"
        })
            Assert.Null(application.GetType(name));
        foreach (var name in new[]
        {
            "OrderHub.Infrastructure.Reopsitories.SupplierRepository",
            "OrderHub.Infrastructure.Reopsitories.DeliverymanRepository",
            "OrderHub.Infrastructure.Reopsitories.RepositoryBase`1",
            "OrderHub.Infrastructure.ReadServices.ProductReadService",
            "OrderHub.Infrastructure.Stores.ProductStore",
            "OrderHub.Infrastructure.Stores.PropertyStore",
            "OrderHub.Infrastructure.ReadServices.LookupService"
        })
            Assert.Null(infrastructure.GetType(name));
        var services = new ServiceCollection();
        OrderHub.Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure(services, "unused");
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IMediator)
            || IsRequestHandler(descriptor.ServiceType));
    }

    [Fact]
    public void Web_dependencies_do_not_resolve_database_persistence()
    {
        var flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        foreach (var type in typeof(AuthenticatedRequestExecutor).Assembly.GetTypes())
        {
            var dependencies = type.GetFields(flags).Select(field => field.FieldType)
                .Concat(type.GetProperties(flags).Select(property => property.PropertyType))
                .Concat(type.GetConstructors(flags).SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType)));
            Assert.DoesNotContain(dependencies, IsDatabaseDependency);
        }
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
        Assert.Empty((await mediator.Send(new GetCategoriesQuery())).Categories);
        Assert.False((await mediator.Send(new GetCategoriesQuery(1))).ParentExists);
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
        && new[] { "MediatR.IRequestHandler`1", "MediatR.IRequestHandler`2", "MediatR.INotificationHandler`1", "MediatR.IStreamRequestHandler`2" }
            .Contains(type.GetGenericTypeDefinition().FullName);

    private static bool IsPersistenceContract(Type type) => type.Assembly == typeof(IRequestExecutor).Assembly && type.IsInterface
        && (type.Name.EndsWith("Reader") || type.Name.EndsWith("Repository") || type == typeof(IUnitOfWork)
            || type == typeof(IDraftReadService) || type == typeof(IUserDetailsService) || type == typeof(IUserListingService));

    private static bool IsDatabaseDependency(Type type) => IsPersistenceContract(type)
        || type == typeof(AppDbContext) || type == typeof(AppDbContextFactory)
        || (type.Assembly == typeof(AppDbContext).Assembly && type.GetInterfaces().Any(IsPersistenceContract))
        || (type.Assembly.GetName().Name?.StartsWith("Microsoft.EntityFrameworkCore") ?? false)
        || (type.HasElementType && IsDatabaseDependency(type.GetElementType()))
        || (type.IsGenericType && type.GetGenericArguments().Any(IsDatabaseDependency));
}
