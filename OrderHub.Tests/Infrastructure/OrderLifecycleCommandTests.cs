using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Tests.Infrastructure;

public class OrderLifecycleCommandTests
{
    [LifecyclePostgresFact]
    public async Task Command_flow_delivers_with_an_incomplete_step_and_preserves_rejected_operation()
    {
        using var fixture = new OrderLifecyclePostgresDatabase();
        int id, userId;
        using (var seed = fixture.CreateDbContext()) (id, userId) = await OrderLifecyclePersistenceTests.Seed(seed);
        using (var start = fixture.CreateDbContext())
        {
            var handler = new OrderLifecycleHandlers(new OrderRepository(start), new UnitOfWork(start),
                Application.AuthorizationTests.Identity(true, userId, false), TimeProvider.System);
            Assert.True((await handler.Handle(new ChangeOrderStatusCommand(id, 1, OrderStatus.OutForDelivery), default)).IsSuccess);
        }
        using (var finish = fixture.CreateDbContext())
        {
            var steps = await finish.OrderDeliverySteps.OrderBy(step => step.StepOrder).ToListAsync();
            var handler = new OrderLifecycleHandlers(new OrderRepository(finish), new UnitOfWork(finish),
                Application.AuthorizationTests.Identity(true, userId, false), TimeProvider.System);
            var result = await handler.Handle(new DeliverOrderDirectlyCommand(id, 2,
                [new(steps[0].Id, DeliveryStatus.Completed), new(steps[1].Id, DeliveryStatus.Pending)], "Delivery confirmed"), default);
            Assert.True(result.IsSuccess, result.ErrorMessage);
        }
        using (var stale = fixture.CreateDbContext())
        {
            var handler = new OrderLifecycleHandlers(new OrderRepository(stale), new UnitOfWork(stale),
                Application.AuthorizationTests.Identity(true, userId, false), TimeProvider.System);
            Assert.Equal("Conflict", (await handler.Handle(new ChangeOrderStatusCommand(id, 2, OrderStatus.Cancelled, "Cancel"), default)).ErrorCode);
        }
        using var verify = fixture.CreateDbContext();
        Assert.Equal(OrderStatus.Delivered, (await verify.Orders.SingleAsync()).OrderStatus);
        Assert.Equal(3, await verify.OrderStatusHistories.CountAsync());
        Assert.Single(await verify.DeliveryStepStatusHistories.ToListAsync());
        var results = await verify.OrderDeliverySteps.OrderBy(step => step.StepOrder).Select(step => step.Status).ToListAsync();
        Assert.Equal(new[] { DeliveryStatus.Completed, DeliveryStatus.Pending }, results);
    }
}
