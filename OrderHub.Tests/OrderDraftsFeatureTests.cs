using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;
using OrderHub.Application.Features.OrderDrafts.Delete;
using OrderHub.Application.Features.OrderDrafts.List;
using OrderHub.Application.Interfaces;

namespace OrderHub.Tests;

public sealed class OrderDraftsFeatureTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Draft_list_filters_sorts_pages_and_reports_stats()
    {
        var drafts = new[]
        {
            Summary("AAAA1111", "Alpha", 2, Now.AddHours(-2)),
            Summary("BBBB2222", "Beta", 0, Now.AddDays(-3)),
            Summary("CCCC3333", "Gamma", 0, Now.AddDays(-40))
        };
        var handler = new GetDraftListQueryHandler(
            new FakeCurrentUser(7), new StubDraftReadService(drafts), new FixedTimeProvider(Now));

        var firstPage = await handler.Handle(
            new GetDraftListQuery(new DraftListRequest(Page: 1, PageSize: 2)), default);

        Assert.Equal(3, firstPage.Stats.TotalCount);
        Assert.Equal(1, firstPage.Stats.ActiveLast24Hours);
        Assert.Equal(2, firstPage.Stats.EmptyCount);
        Assert.Equal(Now.AddDays(-40), firstPage.Stats.OldestUpdatedAt);
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(new[] { "AAAA1111", "BBBB2222" }, firstPage.Items.Select(d => d.ShortId));

        var lastPage = await handler.Handle(
            new GetDraftListQuery(new DraftListRequest(Page: 2, PageSize: 2)), default);
        Assert.Equal(2, lastPage.Page);
        Assert.Equal("CCCC3333", Assert.Single(lastPage.Items).ShortId);

        var clamped = await handler.Handle(
            new GetDraftListQuery(new DraftListRequest(Page: 99, PageSize: 2)), default);
        Assert.Equal(2, clamped.Page);

        var emptyOnly = await handler.Handle(
            new GetDraftListQuery(new DraftListRequest(PageSize: 10, ItemsFilter: DraftItemsFilter.Empty)), default);
        Assert.Equal(new[] { "BBBB2222", "CCCC3333" }, emptyOnly.Items.Select(d => d.ShortId));

        var oldOnly = await handler.Handle(
            new GetDraftListQuery(new DraftListRequest(PageSize: 10, AgeFilter: DraftAgeFilter.OlderThanMonth)), default);
        Assert.Equal("CCCC3333", Assert.Single(oldOnly.Items).ShortId);

        var searched = await handler.Handle(
            new GetDraftListQuery(new DraftListRequest(PageSize: 10, SearchTerm: "#bbbb")), default);
        Assert.Equal("BBBB2222", Assert.Single(searched.Items).ShortId);

        var mostItems = await handler.Handle(
            new GetDraftListQuery(new DraftListRequest(PageSize: 10, Sort: DraftSort.MostItems)), default);
        Assert.Equal("AAAA1111", mostItems.Items[0].ShortId);
    }

    [Fact]
    public async Task Delete_old_drafts_uses_cutoff_and_returns_count()
    {
        var repository = new StubDraftRepository { DeletedCount = 3 };
        var unitOfWork = new StubUnitOfWork();
        var handler = new DeleteOldDraftsCommandHandler(
            new FakeCurrentUser(7), repository, unitOfWork, new FixedTimeProvider(Now));

        var result = await handler.Handle(new DeleteOldDraftsCommand(30), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value);
        Assert.Equal(Now.AddDays(-30), repository.CutoffUtc);
        Assert.Equal(1, unitOfWork.SaveCount);
    }

    [Fact]
    public async Task Delete_old_drafts_rejects_non_positive_days()
    {
        var repository = new StubDraftRepository();
        var handler = new DeleteOldDraftsCommandHandler(
            new FakeCurrentUser(7), repository, new StubUnitOfWork(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new DeleteOldDraftsCommand(0), default);

        Assert.False(result.IsSuccess);
        Assert.False(repository.WasCalled);
    }

    private static OrderDraftSummary Summary(string shortId, string client, int items, DateTime updatedAt) =>
        new(Guid.NewGuid(), shortId, client, items, updatedAt, string.Empty, 0);

    private sealed class FakeCurrentUser(int id) : ICurrentUser
    {
        public int? Id { get; } = id;
        public string? UserName => "tester";
        public string? DisplayName => "Tester";
        public bool IsAuthenticated => true;
        public bool IsAdmin => false;
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }

    private sealed class StubDraftReadService(IReadOnlyList<OrderDraftSummary> drafts) : IDraftReadService
    {
        public Task<Draft> GetOwnedAsync(Guid id, int userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<OrderDraftSummary>> GetUserDraftsAsync(
            int userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(drafts);
    }

    private sealed class StubDraftRepository : IDraftRepository
    {
        public int DeletedCount { get; init; }
        public DateTime CutoffUtc { get; private set; }
        public bool WasCalled { get; private set; }

        public Task StageSaveAsync(Draft draft, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task StageDeleteAsync(Guid id, int userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> StageDeleteBeforeAsync(
            int userId, DateTime cutoffUtc, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            CutoffUtc = cutoffUtc;
            return Task.FromResult(DeletedCount);
        }
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(0);
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
