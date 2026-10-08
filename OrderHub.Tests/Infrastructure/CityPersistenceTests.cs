using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Setup.Cities;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.Setup.Cities;

namespace OrderHub.Tests.Infrastructure;

public class CityPersistenceTests
{
    [Fact]
    public async Task List_is_empty_then_returns_projected_names_in_deterministic_order()
    {
        using var factory = TestDb.CreateFactory();
        var reader = new CityReader(factory);

        Assert.Empty(await reader.GetAllCitiesAsync(default));

        using (var db = factory.CreateDbContext())
        {
            db.Cities.AddRange(new City("Zagazig"), new City("Cairo"), new City("Alexandria"));
            await db.SaveChangesAsync();
        }

        var first = (await reader.GetAllCitiesAsync(default)).ToList();
        var second = (await reader.GetAllCitiesAsync(default)).ToList();

        Assert.Equal(new[] { "Alexandria", "Cairo", "Zagazig" }, first.Select(city => city.Name));
        Assert.Equal(first, second);
        Assert.Null(await reader.GetCityForEditAsync(int.MaxValue, default));
    }

    [Fact]
    public async Task Create_and_update_preserve_exact_name_uniqueness_without_self_collision()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);

        Assert.True((await Execute(provider, new CreateCityCommand(new("  Cairo  ")))).IsSuccess);
        Assert.True((await Execute(provider, new CreateCityCommand(new("Alexandria")))).IsSuccess);

        var cities = (await Execute(provider, new GetAllCitiesQuery())).ToList();
        var cairo = cities.Single(city => city.Name == "Cairo");

        var duplicateCreate = await Execute(provider, new CreateCityCommand(new(" Cairo ")));
        var duplicateUpdate = await Execute(provider, new UpdateCityCommand(new(cairo.Id, "Alexandria")));

        Assert.Equal("هذه المدينة موجودة بالفعل.", duplicateCreate.ErrorMessage);
        Assert.Equal("هذه المدينة موجودة بالفعل.", duplicateUpdate.ErrorMessage);
        Assert.True((await Execute(provider, new UpdateCityCommand(new(cairo.Id, " Cairo ")))).IsSuccess);
        Assert.True((await Execute(provider, new UpdateCityCommand(new(cairo.Id, " Giza ")))).IsSuccess);
        Assert.Equal("Giza", (await Execute(provider, new GetCityForEditQuery(cairo.Id))).Name);

        // The existing varchar unique index compares exact names, rather than lowercased names.
        Assert.True((await Execute(provider, new CreateCityCommand(new("giza")))).IsSuccess);
        Assert.Equal(3, (await Execute(provider, new GetAllCitiesQuery())).Count());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" a ")]
    public async Task Create_and_update_return_business_failures_for_invalid_names(string name)
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);

        Assert.False((await Execute(provider, new CreateCityCommand(new(name)))).IsSuccess);
        Assert.Empty(await Execute(provider, new GetAllCitiesQuery()));
        Assert.True((await Execute(provider, new CreateCityCommand(new("Original")))).IsSuccess);

        var city = Assert.Single(await Execute(provider, new GetAllCitiesQuery()));

        Assert.False((await Execute(provider, new UpdateCityCommand(new(city.Id, name)))).IsSuccess);
        Assert.Equal("Original", (await Execute(provider, new GetCityForEditQuery(city.Id))).Name);
    }

    [Fact]
    public async Task Name_length_boundaries_match_the_domain_and_database()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);

        Assert.False((await Execute(provider, new CreateCityCommand(new(new string('a', 101))))).IsSuccess);
        Assert.True((await Execute(provider, new CreateCityCommand(new("  " + new string('a', 100) + "  ")))).IsSuccess);

        var city = Assert.Single(await Execute(provider, new GetAllCitiesQuery()));

        Assert.False((await Execute(provider, new UpdateCityCommand(new(city.Id, new string('b', 101))))).IsSuccess);
        Assert.True((await Execute(provider, new UpdateCityCommand(new(city.Id, "OK")))).IsSuccess);
    }

    [Fact]
    public async Task Missing_updates_and_deletes_fail_and_unused_city_can_be_deleted()
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);

        var missingUpdate = await Execute(provider, new UpdateCityCommand(new(int.MaxValue, "Missing")));
        var missingDelete = await Execute(provider, new DeleteCityCommand(int.MaxValue));

        Assert.Equal("المدينة غير موجودة.", missingUpdate.ErrorMessage);
        Assert.Equal("المدينة غير موجودة.", missingDelete.ErrorMessage);
        Assert.True((await Execute(provider, new CreateCityCommand(new("Unused")))).IsSuccess);

        var city = Assert.Single(await Execute(provider, new GetAllCitiesQuery()));

        Assert.True((await Execute(provider, new DeleteCityCommand(city.Id))).IsSuccess);
        Assert.Empty(await Execute(provider, new GetAllCitiesQuery()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Referenced_city_cannot_be_deleted(bool deliverymanReference)
    {
        using var factory = TestDb.CreateFactory();
        using var provider = SetupPersistenceTests.Services(factory);
        int cityId;

        using (var db = factory.CreateDbContext())
        {
            var city = new City("Referenced");

            if (deliverymanReference)
            {
                db.Deliverymen.Add(new Deliveryman("Courier", city, "01012345678"));
            }
            else
            {
                db.Addresses.Add(Address.Create("Street", city).Value);
            }

            await db.SaveChangesAsync();
            cityId = city.Id;
        }

        var result = await Execute(provider, new DeleteCityCommand(cityId));
        string expected = deliverymanReference
            ? "لا يمكن حذف المدينة لأنها مرتبطة بمناديب."
            : "لا يمكن حذف المدينة لأنها مرتبطة بعناوين.";

        Assert.False(result.IsSuccess);
        Assert.Equal(expected, result.ErrorMessage);
        Assert.NotNull(await Execute(provider, new GetCityForEditQuery(cityId)));
    }

    [Fact]
    public async Task Read_only_role_can_read_but_all_city_writes_are_denied_without_changes()
    {
        using var factory = TestDb.CreateFactory();
        int userId;
        int cityId;

        using (var db = factory.CreateDbContext())
        {
            var role = new Role("City reader");
            role.SetPermission(PermissionFeature.Cities, PermissionLevel.Read);

            var user = User.Create("cityreader", "hash", "Reader");
            user.AssignRole(role);

            var city = new City("Original");
            db.Users.Add(user);
            db.Cities.Add(city);
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

        Assert.Single(await Execute(provider, new GetAllCitiesQuery()));
        Assert.NotNull(await Execute(provider, new GetCityForEditQuery(cityId)));

        IRequest<Result>[] writes =
        [
            new CreateCityCommand(new("Denied")),
            new UpdateCityCommand(new(cityId, "Denied")),
            new DeleteCityCommand(cityId)
        ];

        foreach (var write in writes)
        {
            await Assert.ThrowsAsync<AuthorizationDeniedException>(() => Execute(provider, write));
        }

        Assert.Equal("Original", (await Execute(provider, new GetCityForEditQuery(cityId))).Name);
        Assert.Single(await Execute(provider, new GetAllCitiesQuery()));
    }

    [Fact]
    public async Task City_reads_propagate_cancellation()
    {
        using var factory = TestDb.CreateFactory();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new CityReader(factory).GetAllCitiesAsync(cancellation.Token));
    }

    private static async Task<T> Execute<T>(ServiceProvider provider, IRequest<T> request)
    {
        using var scope = provider.CreateScope();

        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(request);
    }
}
