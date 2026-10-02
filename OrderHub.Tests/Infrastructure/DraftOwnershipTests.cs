using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.Features.OrderDrafts;

namespace OrderHub.Tests.Infrastructure;

public class DraftOwnershipTests
{
    [Fact]
    public async Task Owner_can_round_trip_but_another_user_cannot_read_delete_or_claim_draft()
    {
        using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        var factory = new AppDbContextFactory(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        using var db = factory.CreateDbContext();
        // Exercise the real draft queries without unrelated PostgreSQL-only schema constraints.
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE order_drafts (Id TEXT PRIMARY KEY, UserId INTEGER NOT NULL, Data TEXT NOT NULL, LastModified TEXT NOT NULL)");
        var store = new DatabaseDraftStore(factory);
        var owner = new DraftService(store, Identity(11));
        var other = new DraftService(store, Identity(22));
        var context = new DraftContext(Guid.NewGuid());
        var data = new OrderDraftData(null, "Client", null, null, null, null, [], []);
        await owner.SaveAsync(context, data);

        Assert.Single(await owner.GetAllAsync());
        Assert.Empty(await other.GetAllAsync());
        Assert.Null(await other.GetAsync(context));
        await other.DeleteAsync(context);
        Assert.NotNull(await owner.GetAsync(context));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => other.SaveAsync(context, data with { ClientName = "Claimed" }));
        Assert.Equal("Client", (await owner.GetAsync(context)).Data.ClientName);

        await owner.SaveAsync(context, data with { ClientName = "Updated" });
        Assert.Equal("Updated", (await owner.GetAsync(context)).Data.ClientName);
        await owner.DeleteAsync(context);
        Assert.Null(await owner.GetAsync(context));
    }

    [Fact]
    public async Task Anonymous_operations_fail_before_accessing_store()
    {
        var store = new Mock<IDraftStore>(MockBehavior.Strict);
        var service = new DraftService(store.Object, Identity(null));
        var context = new DraftContext(Guid.NewGuid());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAllAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetAsync(context));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteAsync(context));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SaveAsync(context,
            new OrderDraftData(null, "", null, null, null, null, [], [])));
        store.VerifyNoOtherCalls();
    }

    private static ICurrentUser Identity(int? id)
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(x => x.GetUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(id);
        return user.Object;
    }
}
