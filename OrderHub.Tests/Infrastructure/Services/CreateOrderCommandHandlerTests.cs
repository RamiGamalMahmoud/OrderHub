using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.CommandHandlers.Orders;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.OrderCommands;
using static OrderHub.Application.DTOs.OrderDtos;

namespace OrderHub.Tests.Infrastructure.Services;

public class CreateOrderCommandHandlerTests
{
    private (AppDbContext Context, DbContextOptions<AppDbContext> Options) CreateInMemoryDb()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return (context, options);
    }

    [Fact]
    public async Task Handle_Should_Persist_DeliveryChain_Steps()
    {
        var (dbContext, options) = CreateInMemoryDb();
        using (dbContext)
        {

            var client = TestData.CreateClient();
            var deliveryman = TestData.CreateDeliveryman();
            var shippingCarrier = TestData.CreateShippingCarrier();

            dbContext.Clients.Add(client);
            dbContext.Deliverymen.Add(deliveryman);
            dbContext.ShippingCarriers.Add(shippingCarrier);
            await dbContext.SaveChangesAsync();

            var handler = new CreateOrderCommandHandler(new AppDbContextFactory(options));
            var command = new CreateOrderCommand(new OrderCreateDto(
                client.Id,
                1,
                DeliveryMethod.DeliveryChain,
                null,
                null,
                [],
                [
                    new OrderDeliveryStepCreateDto(1, DeliveryMethod.DeliveryMan, deliveryman.Id),
                    new OrderDeliveryStepCreateDto(2, DeliveryMethod.ShippingCompany, shippingCarrier.Id)
                ],
                null));

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.True(result.IsSuccess);

            var savedOrder = await dbContext.Orders
                .Include(order => order.DeliverySteps)
                .SingleAsync(order => order.Id == result.Value);

            Assert.Equal(DeliveryMethod.DeliveryChain, savedOrder.DeliveryMethod);
            Assert.Null(savedOrder.DeliverymanId);
            Assert.Null(savedOrder.ShippingCarrierId);
            Assert.Equal(2, savedOrder.DeliverySteps.Count);

            var steps = savedOrder.DeliverySteps.OrderBy(step => step.StepOrder).ToList();
            Assert.Equal(DeliveryMethod.DeliveryMan, steps[0].DeliveryMethod);
            Assert.Equal(deliveryman.Id, steps[0].DeliverymanId);
            Assert.Null(steps[0].ShippingCarrierId);
            Assert.Equal(DeliveryMethod.ShippingCompany, steps[1].DeliveryMethod);
            Assert.Equal(shippingCarrier.Id, steps[1].ShippingCarrierId);
            Assert.Null(steps[1].DeliverymanId);
        }
    }
}
