using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using OrderHub.Application.Features.Orders.Contracts;
using OrderHub.Application.Features.Orders.Create;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.ValueObjects;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Tests.Infrastructure;

public class OrderCreationConcurrencyTests
{
    [LifecyclePostgresFact]
    public async Task Parallel_creation_allocates_distinct_numbers_and_repeated_request_creates_one_order()
    {
        using var fixture = new OrderLifecyclePostgresDatabase();
        int userId, clientId, productId;
        using (var seed = fixture.CreateDbContext())
        {
            var (_, actor) = await OrderLifecyclePersistenceTests.Seed(seed);
            userId = actor;
            clientId = (await seed.Clients.SingleAsync()).Id;
            var product = new Product("Product", "P1", new Category("Category"), new Money(10));
            seed.Products.Add(product);
            await seed.SaveChangesAsync();
            productId = product.Id;
            var numbered = new Order(clientId, $"ORD-{DateTime.Now:yyyy-MM-dd}-9999");
            numbered.UpdateHeader(clientId, OrderSource.Online, DeliveryMethod.Pickup, null, null, null);
            numbered.InitializeHistory(new(userId, "Operator", numbered.CreatedAt));
            seed.Orders.Add(numbered);
            await seed.SaveChangesAsync();
        }
        var input = new OrderInput.Order(clientId, OrderSource.Online, DeliveryMethod.Pickup, null, null,
            [new(productId, "Saved product", 1, 10, 0, null, null, [])], [], null);
        async Task<int> Create(Guid key)
        {
            using var db = fixture.CreateDbContext();
            var handler = new CreateOrderCommandHandler(new OrderRepository(db), new UnitOfWork(db),
                new Mock<IQuotationRepository>(MockBehavior.Strict).Object,
                new Mock<IProformaInvoiceRepository>(MockBehavior.Strict).Object,
                Application.AuthorizationTests.Identity(true, userId, false), new Mock<IDraftRepository>(MockBehavior.Strict).Object);
            var result = await handler.Handle(new(null, input, key), default);
            Assert.True(result.IsSuccess, result.ErrorMessage);
            return result.Value;
        }
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var parallel = await Task.WhenAll(Create(first), Create(second));
        Assert.NotEqual(parallel[0], parallel[1]);
        var same = Guid.NewGuid();
        var repeated = await Task.WhenAll(Create(same), Create(same));
        Assert.Equal(repeated[0], repeated[1]);
        using var verify = fixture.CreateDbContext();
        var created = await verify.Orders.Where(order => order.CreationRequestId != null).ToListAsync();
        Assert.Equal(3, created.Count);
        Assert.Equal(3, created.Select(order => order.OrderNumber).Distinct().Count());
        Assert.Equal(5, await verify.OrderStatusHistories.CountAsync()); // including seed creations
        Assert.Contains(created, order => order.OrderNumber.EndsWith("-10000"));
        Assert.Contains(created, order => order.OrderNumber.EndsWith("-10001"));
        Assert.Equal(3, await verify.OrderItems.CountAsync());
    }
}
