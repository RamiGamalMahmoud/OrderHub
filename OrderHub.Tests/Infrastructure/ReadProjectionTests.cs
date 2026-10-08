using OrderHub.Application.Features.Orders.GetItemEditor;
using OrderHub.Application.Features.Orders.GetItemsEditor;
using OrderHub.Application.Features.Orders.GetDetails;
using OrderHub.Application.Features.Orders.GetPage;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Products.List;
using OrderHub.Application.Features.Products.Search;
using OrderHub.Application.Features.Setup.Clients;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.Orders;

namespace OrderHub.Tests.Infrastructure;

public class ReadProjectionTests
{
    [Fact]
    public async Task Editor_projection_preserves_historical_metadata_and_has_no_per_item_queries()
    {
        using var factory = TestDb.CreateFactory();
        var ids = await Seed(factory);
        using var connectionOwner = factory.CreateDbContext();
        var counter = new ReadCounter();
        var counted = new CountingFactory(connectionOwner.Database.GetDbConnection(), counter);
        var reader = new OrderReader(counted);
        var first = await reader.GetForEditAsync(ids.FirstOrder, default);
        var singleCount = counter.Count;
        Assert.True(singleCount > 0);
        counter.Count = 0;
        var many = await reader.GetForEditAsync(ids.SecondOrder, default);
        Assert.Equal(singleCount, counter.Count);
        Assert.Equal(5, many.OrderItems.Count());
        foreach (var item in first.OrderItems.Concat(many.OrderItems))
        {
            Assert.Equal("Saved product", item.ProductName);
            Assert.Equal(12.5m, item.UnitPrice);
            Assert.Equal(15m, item.VatRate);
            var current = Assert.Single(item.Properties, property => property.PropertyId == ids.CurrentProperty);
            Assert.True(current.IsRequired);
            Assert.Equal("Saved", current.SelectedValue);
            Assert.Equal(new[] { "Other", "Saved" }, current.Options.Select(option => option.Value).OrderBy(value => value));
            var historical = Assert.Single(item.Properties, property => property.PropertyId == ids.HistoricalProperty);
            Assert.False(historical.IsRequired);
            Assert.Equal("false", historical.SelectedValue);
            Assert.Equal(PropertyType.Boolean, historical.PropertyType);
        }
        Assert.Null(await reader.GetForEditAsync(int.MaxValue, default));
    }

    [Fact]
    public async Task Lists_search_pagination_and_order_details_remain_usable_through_Application()
    {
        using var factory = TestDb.CreateFactory();
        var ids = await Seed(factory);
        using var provider = SetupPersistenceTests.Services(factory);
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var products = Assert.Single(await mediator.Send(new ListProductsQuery()));
        Assert.Equal("Current product", products.Name);
        var productReader = scope.ServiceProvider.GetRequiredService<IProductReader>();
        Assert.Single(await mediator.Send(new SearchProductsQuery(" Current ")));
        Assert.Single(await productReader.GetProductByCategoryAsync(ids.Category));
        Assert.Single(await mediator.Send(new GetClientsByNameQuery("01012345678")));
        var clientPage = await mediator.Send(new GetAllClientsPagedQuery(1, 1));
        Assert.Equal(1, clientPage.TotalCount);
        Assert.Single(clientPage.Items);
        var page = await mediator.Send(new GetOrdersPageQuery(1, 1, "ORD-"));
        Assert.Equal(2, page.TotalCount);
        Assert.Single(page.Items);
        var filtered = await mediator.Send(new GetOrdersPageQuery(SearchTerm: "ORD-ONE"));
        Assert.Single(filtered.Items);
        var details = await mediator.Send(new GetOrderDetailsQuery(ids.FirstOrder));
        Assert.Equal("ORD-ONE", details.Header.OrderNumber);
        Assert.Equal(25m, details.Payment.ProductsTotal);
        Assert.Equal(25m, Assert.Single(details.Items).Total);
        var one = await mediator.Send(new GetOrderItemEditorQuery(ids.Product));
        Assert.Equal("Current product", one.Name);
        Assert.Single(await mediator.Send(new GetOrderItemsEditorQuery([ids.Product])));
        Assert.Empty(scope.ServiceProvider.GetRequiredService<AppDbContext>().ChangeTracker.Entries());
    }

    private static async Task<(int FirstOrder, int SecondOrder, int CurrentProperty, int HistoricalProperty, int Category, int Product)> Seed(TestDbFactory factory)
    {
        using var context = factory.CreateDbContext();
        var city = new City("City");
        var client = new Client("Client", "Street", city, "01012345678", "+20");
        var category = new Category("Category");
        var current = Property.Create("Current list", PropertyType.List, options: ["Saved", "Other"]);
        var historical = Property.Create("Removed boolean", PropertyType.Boolean);
        context.AddRange(client, category, current, historical);
        await context.SaveChangesAsync();
        var product = new Product("Current product", "P1", category, new Money(99));
        product.AddProperty(current.Id, true);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var first = Build("ORD-ONE", 1);
        var second = Build("ORD-MANY", 5);
        context.Orders.AddRange(first, second);
        await context.SaveChangesAsync();
        return (first.Id, second.Id, current.Id, historical.Id, category.Id, product.Id);

        Order Build(string number, int count)
        {
            var order = new Order(client.Id, number);
            order.UpdateHeader(client.Id, OrderSource.Online, DeliveryMethod.Pickup, null, null, 1);
            for (var index = 0; index < count; index++)
                Assert.True(order.AddOrderItem(product.Id, "Saved product", 12.5m, 2, 15m, "", null,
                    [new(current.Id, "Saved"), new(historical.Id, "false")]).IsSuccess);
            return order;
        }
    }

    private sealed class CountingFactory(DbConnection connection, ReadCounter counter) : AppDbContextFactory("")
    {
        public override AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection).UseSnakeCaseNamingConvention().AddInterceptors(counter).Options);
    }

    private sealed class ReadCounter : DbCommandInterceptor
    {
        public int Count { get; set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Count++;
            return ValueTask.FromResult(result);
        }
    }
}
