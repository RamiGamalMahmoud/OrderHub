using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OrderHub.Application;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.OrderDrafts.Save;
using OrderHub.Application.Features.OrderDrafts.Get;
using OrderHub.Application.Features.OrderDrafts.List;
using OrderHub.Application.Features.OrderDrafts.Delete;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;
using OrderHub.Infrastructure;
using OrderHub.Infrastructure.DependencyInjection;
using OrderHub.Infrastructure.Features.OrderDrafts;
using OrderHub.Infrastructure.Models;
using OrderHub.Infrastructure.Services;

namespace OrderHub.Tests.Infrastructure;

public class DraftOwnershipTests
{
    [Fact]
    public async Task Owner_can_round_trip_but_another_user_cannot_read_delete_or_claim_draft()
    {
        using var fixture = new Fixture();
        var id = Guid.NewGuid();
        var data = Data();
        Assert.True((await fixture.Save(id, data, 11)).IsSuccess);
        Assert.Single(await fixture.List(11));
        Assert.Empty(await fixture.List(22));
        Assert.Null(await fixture.Get(id, 22));
        Assert.True((await fixture.Delete(id, 22)).IsSuccess);
        Assert.NotNull(await fixture.Get(id, 11));
        var foreign = await fixture.Save(id, data with { ClientName = "Claimed" }, 22);
        Assert.False(foreign.IsSuccess);
        Assert.Equal("The draft is not available to this user.", foreign.ErrorMessage);
        Assert.Equal("Client", (await fixture.Get(id, 11)).Data.ClientName);
        Assert.True((await fixture.Save(id, data with { ClientName = "Updated" }, 11)).IsSuccess);
        using (var db = fixture.CreateDbContext()) Assert.Single(await db.DraftRecords.ToListAsync());
        Assert.Equal("Updated", (await fixture.Get(id, 11)).Data.ClientName);
        Assert.True((await fixture.Delete(id, 11)).IsSuccess);
        Assert.Null(await fixture.Get(id, 11));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Anonymous_operations_fail_before_accessing_persistence(int? userId)
    {
        var repository = new Mock<IDraftRepository>(MockBehavior.Strict);
        var reads = new Mock<IDraftReadService>(MockBehavior.Strict);
        var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var user = Identity(userId);
        var id = Guid.NewGuid();
        Assert.False((await new SaveDraftCommandHandler(user, repository.Object, unit.Object).Handle(new(id, Data()), default)).IsSuccess);
        Assert.False((await new DeleteDraftCommandHandler(user, repository.Object, unit.Object).Handle(new(id), default)).IsSuccess);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new GetDraftQueryHandler(user, reads.Object).Handle(new(id), default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new GetDraftListQueryHandler(user, reads.Object).Handle(new(), default));
        repository.VerifyNoOtherCalls();
        reads.VerifyNoOtherCalls();
        unit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Serialization_preserves_incomplete_data_and_database_envelope_is_authoritative()
    {
        using var fixture = new Fixture();
        var id = Guid.NewGuid();
        var data = Data();
        Assert.True((await fixture.Save(id, data, 11)).IsSuccess);
        var actual = await fixture.Get(id, 11);
        Assert.Equal(JsonSerializer.Serialize(data), JsonSerializer.Serialize(actual.Data));
        Assert.Equal(11, actual.UserId);
        var timestamp = DateTime.UtcNow.AddHours(-1);
        using (var db = fixture.CreateDbContext())
        {
            var record = await db.DraftRecords.SingleAsync();
            record.LastModified = timestamp;
            record.Data = JsonSerializer.Serialize(actual with { Id = Guid.NewGuid(), UserId = 999, LastModified = DateTime.MinValue });
            await db.SaveChangesAsync();
        }
        actual = await fixture.Get(id, 11);
        Assert.Equal(id, actual.Id);
        Assert.Equal(11, actual.UserId);
        Assert.Equal(timestamp, actual.LastModified);
    }

    [Fact]
    public async Task List_is_owner_filtered_ordered_and_skips_malformed_data()
    {
        using var fixture = new Fixture();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        await fixture.Save(first, Data(), 11);
        await fixture.Save(second, Data(), 11);
        await fixture.Save(Guid.NewGuid(), Data(), 22);
        using (var db = fixture.CreateDbContext())
        {
            var row = await db.DraftRecords.SingleAsync(r => r.Id == first);
            row.LastModified = DateTime.UtcNow.AddDays(-1);
            db.DraftRecords.Add(new DraftRecord { Id = Guid.NewGuid(), UserId = 11, Data = "{}", LastModified = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }
        Assert.Equal(new[] { second, first }, (await fixture.List(11)).Select(d => d.Id));
    }

    [Fact]
    public async Task Repository_stages_without_committing_and_successful_commands_commit_once()
    {
        using var fixture = new Fixture();
        var id = Guid.NewGuid();
        using (var db = fixture.CreateDbContext())
        {
            var repo = new DraftRepository(db);
            await repo.StageSaveAsync(new Draft(id, 11, DateTime.UtcNow, Data()));
            Assert.Null(await fixture.Get(id, 11));
            var unit = Unit(db);
            await unit.Object.SaveChangesAsync(default);
            unit.Invocations.Clear();
            Assert.True((await new SaveDraftCommandHandler(Identity(11), repo, unit.Object).Handle(new(id, Data()), default)).IsSuccess);
            unit.Verify(u => u.SaveChangesAsync(default), Times.Once());
        }
        using (var db = fixture.CreateDbContext())
        {
            var unit = Unit(db);
            Assert.True((await new DeleteDraftCommandHandler(Identity(11), new DraftRepository(db), unit.Object).Handle(new(id), default)).IsSuccess);
            unit.Verify(u => u.SaveChangesAsync(default), Times.Once());
        }
        Assert.Null(await fixture.Get(id, 11));
    }

    [Fact]
    public async Task Foreign_overwrite_never_commits_and_failed_database_save_keeps_existing_data()
    {
        using var fixture = new Fixture();
        var id = Guid.NewGuid();
        await fixture.Save(id, Data(), 11);
        using (var db = fixture.CreateDbContext())
        {
            var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
            Assert.False((await new SaveDraftCommandHandler(Identity(22), new DraftRepository(db), unit.Object).Handle(new(id, Data()), default)).IsSuccess);
            unit.VerifyNoOtherCalls();
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_draft_update BEFORE UPDATE ON order_drafts BEGIN SELECT RAISE(ABORT, 'blocked'); END");
        }
        Assert.False((await fixture.Save(id, Data() with { ClientName = "Changed" }, 11)).IsSuccess);
        Assert.Equal("Client", (await fixture.Get(id, 11)).Data.ClientName);
    }

    [Fact]
    public void Canonical_draft_repository_shares_scoped_context_and_removed_abstractions_are_absent()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure("Host=localhost;Database=draft_test;Username=test;Password=test");
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDraftRepository>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Contains(typeof(DraftRepository).GetFields(BindingFlags.NonPublic | BindingFlags.Instance),
            field => ReferenceEquals(context, field.GetValue(repository)));
        var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Assert.Same(context, typeof(UnitOfWork).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(unit));
        Assert.DoesNotContain(typeof(DraftRepository).GetMethods(), m => m.Name.Contains("SaveChanges"));
        Assert.DoesNotContain(typeof(Draft).Assembly.GetTypes(), type => type.Name is "IDraftStore" or "IDraftService" or "DraftContext");
        Assert.DoesNotContain(typeof(DraftRepository).Assembly.GetTypes(), type => type.Name is "DatabaseDraftStore" or "DraftService");
        Assert.DoesNotContain(typeof(SaveDraftCommandHandler).GetConstructors().SelectMany(c => c.GetParameters()),
            parameter => parameter.ParameterType.Assembly == typeof(DraftRepository).Assembly);
        Assert.DoesNotContain(typeof(SaveDraftCommand).GetProperties(), property => property.Name == "UserId");
    }

    private static OrderDraftData Data() => new(null, "Client", OrderSource.Online, null, null, null, null,
        [new(7, "Product", "Category", 12.34m, 2, 14.75m, null, "", [new(10, null)])],
        [new(1, DeliveryMethod.DeliveryMan, null)]);
    private static ICurrentUser Identity(int? id)
    {
        var user = new Mock<ICurrentUser>();
        user.Setup(u => u.GetUserIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(id);
        return user.Object;
    }
    private static Mock<IUnitOfWork> Unit(AppDbContext db)
    {
        var unit = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var real = new UnitOfWork(db);
        unit.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns((CancellationToken token) => real.SaveChangesAsync(token));
        return unit;
    }
    private sealed class Fixture : AppDbContextFactory, IDisposable
    {
        private readonly SqliteConnection _connection = new("Filename=:memory:;Foreign Keys=False");
        public Fixture() : base("")
        {
            _connection.Open();
            using var db = CreateDbContext();
            db.Database.EnsureCreated();
        }
        public override AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).UseSnakeCaseNamingConvention().Options);
        public Task<Draft> Get(Guid id, int userId) => new GetDraftQueryHandler(Identity(userId), new DraftReadService(this)).Handle(new(id), default);
        public Task<System.Collections.Generic.IReadOnlyList<OrderDraftSummary>> List(int userId) => new GetDraftListQueryHandler(Identity(userId), new DraftReadService(this)).Handle(new(), default);
        public async Task<OrderHub.Domain.Common.Result> Save(Guid id, OrderDraftData data, int userId)
        {
            using var db = CreateDbContext();
            return await new SaveDraftCommandHandler(Identity(userId), new DraftRepository(db), new UnitOfWork(db)).Handle(new(id, data), default);
        }
        public async Task<OrderHub.Domain.Common.Result> Delete(Guid id, int userId)
        {
            using var db = CreateDbContext();
            return await new DeleteDraftCommandHandler(Identity(userId), new DraftRepository(db), new UnitOfWork(db)).Handle(new(id), default);
        }
        public void Dispose() => _connection.Dispose();
    }
}
