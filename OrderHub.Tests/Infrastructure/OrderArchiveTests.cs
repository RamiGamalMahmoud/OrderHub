using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Orders.Archive;
using OrderHub.Application.Features.Orders.Lifecycle;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Reopsitories;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Tests.Infrastructure;

public class OrderArchiveTests
{
    [LifecyclePostgresFact]
    public async Task Archive_preserves_invoice_history_steps_and_business_references_and_has_no_restore()
    {
        using var fixture = new OrderLifecyclePostgresDatabase();
        int id, userId;
        using (var seed = fixture.CreateDbContext())
        {
            (id, userId) = await OrderLifecyclePersistenceTests.Seed(seed);
            seed.Invoices.Add(OrderHub.Domain.Models.CommercialDocuments.Invoice.Create("INV", DateTime.UtcNow,
                "Client snapshot", "Phone", "Address", 10, 0, 10, id));
            await seed.SaveChangesAsync();
        }
        using (var archive = fixture.CreateDbContext())
        {
            var handler = new OrderLifecycleHandlers(new OrderRepository(archive), new UnitOfWork(archive),
                Application.AuthorizationTests.Identity(true, userId, false), TimeProvider.System);
            Assert.True((await handler.Handle(new ArchiveOrderCommand(id, 1, "Duplicate"), default)).IsSuccess);
            Assert.False((await handler.Handle(new ChangeOrderStatusCommand(id, 2, OrderStatus.OutForDelivery), default)).IsSuccess);
            Assert.False((await handler.Handle(new ArchiveOrderCommand(id, 2, "Again"), default)).IsSuccess);
        }
        using var verify = fixture.CreateDbContext();
        var root = await verify.Orders.SingleAsync();
        Assert.True(root.IsArchived);
        Assert.Equal(OrderStatus.Processing, root.OrderStatus);
        Assert.Equal("Duplicate", root.ArchiveReason);
        Assert.Equal(userId, root.ArchivedByUserId);
        Assert.Equal(DateTimeKind.Utc, root.ArchivedAtUtc.Value.Kind);
        Assert.Single(await verify.OrderStatusHistories.ToListAsync());
        Assert.Equal(2, await verify.OrderDeliverySteps.CountAsync());
        var invoice = await new InvoiceRepository(verify).GetByOrderId(id);
        Assert.Equal(id, invoice.Order.Id);
        Assert.True(invoice.Order.IsArchived);
        Assert.True(await new OrderHub.Infrastructure.Features.Setup.Clients.ClientRepository(verify).HasBusinessReferencesAsync(root.ClientId, default));
    }
}
