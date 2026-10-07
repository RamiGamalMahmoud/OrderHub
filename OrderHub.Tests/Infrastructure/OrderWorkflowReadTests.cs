using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure.Features.Orders;

namespace OrderHub.Tests.Infrastructure;

public class OrderWorkflowReadTests
{
    [LifecyclePostgresFact]
    public async Task Reads_share_domain_actions_page_history_and_keep_archive_accessible()
    {
        using var fixture = new OrderLifecyclePostgresDatabase();
        int id, userId;
        using (var seed = fixture.CreateDbContext()) (id, userId) = await OrderLifecyclePersistenceTests.Seed(seed);
        var reader = new OrderReader(fixture);
        var initial = await reader.GetWorkflowAsync(id, default);
        Assert.True(initial.CanEdit);
        Assert.Contains(OrderStatus.OutForDelivery, initial.AllowedStatuses);
        Assert.All(initial.Steps, step => Assert.Empty(step.AllowedStatuses));
        using (var start = fixture.CreateDbContext())
        {
            var root = await start.Orders.Include(order => order.DeliverySteps).SingleAsync();
            root.TransitionTo(OrderStatus.OutForDelivery, new(userId, "Operator", DateTime.UtcNow));
            root.TransitionStep(root.DeliverySteps.OrderBy(step => step.StepOrder).First().Id,
                DeliveryStatus.InProgress, new(userId, "Operator", DateTime.UtcNow));
            await start.SaveChangesAsync();
        }
        var active = await reader.GetWorkflowAsync(id, default);
        Assert.False(active.CanEdit);
        Assert.DoesNotContain(OrderStatus.DeliveryFailed, active.AllowedStatuses);
        Assert.Contains(DeliveryStatus.Completed, active.Steps[0].AllowedStatuses);
        Assert.Empty(active.Steps[1].AllowedStatuses);
        Assert.NotNull(active.Steps[1].BlockingReason);
        Assert.Equal("Operator", active.Steps[0].LastChangedBy);
        var firstPage = await reader.GetHistoryAsync(id, null, 1, 1, default);
        var secondPage = await reader.GetHistoryAsync(id, null, 2, 1, default);
        Assert.Equal(2, firstPage.TotalCount);
        Assert.NotEqual(firstPage.Items.Single().Id, secondPage.Items.Single().Id);
        Assert.Single((await reader.GetHistoryAsync(id, active.Steps[0].Id, 1, 20, default)).Items);
        Assert.Empty((await reader.GetHistoryAsync(id + 999, active.Steps[0].Id, 1, 20, default)).Items);
        using (var close = fixture.CreateDbContext())
        {
            var root = await close.Orders.Include(order => order.DeliverySteps).SingleAsync();
            root.TransitionTo(OrderStatus.Cancelled, new(userId, "Operator", DateTime.UtcNow), "Cancel");
            root.Archive(new(userId, "Operator", DateTime.UtcNow), "Archive");
            await close.SaveChangesAsync();
        }
        Assert.Empty((await reader.GetPageAsync(1, 20, null, null, null, null, null, default)).Items);
        Assert.Single((await reader.GetPageAsync(1, 20, null, null, null, null, null, default, archivedOnly: true)).Items);
        var archived = await reader.GetWorkflowAsync(id, default);
        Assert.True(archived.IsArchived);
        Assert.Empty(archived.AllowedStatuses);
        Assert.All(archived.Steps, step => Assert.Empty(step.AllowedStatuses));
        Assert.NotNull(await reader.GetDetailsAsync(id, default));
        Assert.Null(await reader.GetDetailsAsync(int.MaxValue, default));
        Assert.Null(await reader.GetWorkflowAsync(int.MaxValue, default));
    }

    [LifecyclePostgresFact]
    public async Task Missing_legacy_method_is_read_safe_and_utc_bounds_are_not_truncated()
    {
        using var fixture = new OrderLifecyclePostgresDatabase();
        int id;
        using (var seed = fixture.CreateDbContext()) (id, _) = await OrderLifecyclePersistenceTests.Seed(seed);
        using (var update = fixture.CreateDbContext())
            await update.Database.ExecuteSqlRawAsync("UPDATE orders SET delivery_method = NULL");
        var reader = new OrderReader(fixture);
        var details = await reader.GetDetailsAsync(id, default);
        Assert.Equal("غير محددة", details.Delivery.DeliveryMethod);
        var state = await reader.GetWorkflowAsync(id, default);
        Assert.NotNull(state.ReadinessError);
        Assert.DoesNotContain(OrderStatus.OutForDelivery, state.AllowedStatuses);
        var at = state.CreatedAt;
        Assert.Single((await reader.GetPageAsync(1, 20, null, at.AddMinutes(-1), at.AddMinutes(1), null, null, default)).Items);
        Assert.Empty((await reader.GetPageAsync(1, 20, null, at.AddMinutes(1), at.AddMinutes(2), null, null, default)).Items);
        Assert.Empty((await reader.GetPageAsync(int.MaxValue, 100, null, null, null, null, null, default)).Items);
    }
}
