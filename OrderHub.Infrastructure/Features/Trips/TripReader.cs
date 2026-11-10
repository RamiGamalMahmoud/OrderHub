#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Common;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.Trips;
using OrderHub.Infrastructure.Extensions;

namespace OrderHub.Infrastructure.Features.Trips;

internal sealed class TripReader(
    AppDbContextFactory contextFactory,
    IStandaloneTripRepository repository) : ITripReader
{
    private static readonly TimeZoneInfo RiyadhTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Riyadh");

    public async Task<PagedResult<TripListItem>> GetPageAsync(
        TripSearchCriteria criteria,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        using var context = contextFactory.CreateDbContext();

        page = Math.Max(1, page);
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 25;

        var query = ApplyCriteria(context.StandaloneTrips.AsNoTracking(), criteria)
            .Select(trip => new
            {
                trip.Id,
                Source = TripSource.Standalone,
                trip.TripNumber,
                trip.SequenceYear,
                trip.SequenceMonth,
                Title = (string?)trip.Title,
                trip.DeliverymanId,
                DeliverymanName = trip.Deliveryman.Name.Value,
                FromType = (TripPartyType?)trip.From.PartyType,
                FromName = (string?)trip.From.Name,
                ToType = (TripPartyType?)trip.To.PartyType,
                ToName = (string?)trip.To.Name,
                trip.ScheduledAtUtc,
                trip.Status,
                trip.StartedAtUtc,
                trip.CreatedAt,
                trip.Version,
                AttemptCount = trip.Attempts.Count,
                LatestAttemptNumber = trip.Attempts.Any()
                    ? trip.Attempts.Max(attempt => attempt.AttemptNumber)
                    : (int?)null,
                HasOpenStarted = trip.Attempts.Any(attempt =>
                    attempt.Status == WorkRecordStatus.Pending && attempt.StartedAtUtc != null),
                HasOpenUnstarted = trip.Attempts.Any(attempt =>
                    attempt.Status == WorkRecordStatus.Pending && attempt.StartedAtUtc == null),
                HasFailedAttempt = trip.Attempts.Any(attempt => attempt.Status == WorkRecordStatus.Failed),
                OrderId = (int?)null,
                OrderNumber = (string?)null
            });

        if (criteria.PartyType is null
            && criteria.ScheduledFrom is null
            && criteria.ScheduledTo is null)
        {
            var orderRows = from trip in context.DeliveryTrips.AsNoTracking()
                            join deliveryman in context.Deliverymen.AsNoTracking()
                                on trip.NumberingPartyId equals deliveryman.Id
                            where trip.OrderId != null || trip.DeliveryStepId != null
                            select new
                            {
                                trip,
                                DeliverymanName = deliveryman.Name.Value,
                                OrderId = trip.OrderId ?? (trip.DeliveryStep != null ? trip.DeliveryStep.OrderId : null),
                                OrderNumber = trip.Order != null
                                    ? trip.Order.OrderNumber
                                    : trip.DeliveryStep != null ? trip.DeliveryStep.Order.OrderNumber : (string?)null
                            };

            if (criteria.DeliverymanId is > 0)
            {
                int deliverymanId = criteria.DeliverymanId.Value;
                orderRows = orderRows.Where(row => row.trip.NumberingPartyId == deliverymanId);
            }

            var states = criteria.DisplayStates?
                .Where(state => Enum.IsDefined(state))
                .ToHashSet();

            if (states is { Count: > 0 })
            {
                bool includeInProgress = states.Contains(TripDisplayState.InProgress);
                bool includeCompleted = states.Contains(TripDisplayState.Completed);
                bool includeFailed = states.Contains(TripDisplayState.Failed);
                bool includeCancelled = states.Contains(TripDisplayState.Cancelled);

                // Order-bound trips have no attempts; Pending is their active state.
                orderRows = orderRows.Where(row =>
                    (includeInProgress && row.trip.Status == WorkRecordStatus.Pending)
                    || (includeCompleted && row.trip.Status == WorkRecordStatus.Completed)
                    || (includeFailed && row.trip.Status == WorkRecordStatus.Failed)
                    || (includeCancelled && row.trip.Status == WorkRecordStatus.Cancelled));
            }

            if (criteria.CreatedFrom is { } createdFrom)
            {
                DateTime fromUtc = RiyadhDayStartUtc(createdFrom);
                orderRows = orderRows.Where(row => row.trip.CreatedAt >= fromUtc);
            }

            if (criteria.CreatedTo is { } createdTo)
            {
                DateTime toExclusiveUtc = RiyadhDayStartUtc(createdTo.AddDays(1));
                orderRows = orderRows.Where(row => row.trip.CreatedAt < toExclusiveUtc);
            }

            string? search = criteria.Search?.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                string lower = search.ToLowerInvariant();
                long? number = long.TryParse(
                    search.TrimStart('#'),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long parsedNumber)
                    ? parsedNumber
                    : null;

                orderRows = orderRows.Where(row =>
                    row.DeliverymanName.ToLower().Contains(lower)
                    || (row.OrderNumber != null && row.OrderNumber.ToLower().Contains(lower))
                    || (number != null && row.trip.WorkRecordNumber == number));
            }

            var orderQuery = orderRows.Select(row => new
            {
                row.trip.Id,
                Source = TripSource.OrderBound,
                TripNumber = row.trip.WorkRecordNumber,
                row.trip.SequenceYear,
                row.trip.SequenceMonth,
                Title = (string?)null,
                DeliverymanId = row.trip.NumberingPartyId,
                row.DeliverymanName,
                FromType = (TripPartyType?)null,
                FromName = (string?)null,
                ToType = (TripPartyType?)null,
                ToName = (string?)null,
                ScheduledAtUtc = (DateTime?)null,
                row.trip.Status,
                StartedAtUtc = (DateTime?)null,
                row.trip.CreatedAt,
                Version = 0,
                AttemptCount = 0,
                LatestAttemptNumber = (int?)null,
                HasOpenStarted = false,
                HasOpenUnstarted = false,
                HasFailedAttempt = row.trip.Status == WorkRecordStatus.Failed,
                row.OrderId,
                row.OrderNumber
            });

            query = query.Concat(orderQuery);
        }

        var projected = await query
            .OrderByDescending(row => row.CreatedAt)
            .ThenByDescending(row => row.Id)
            .ThenByDescending(row => row.Source)
            .ToPagedResultAsync(page, pageSize, cancellationToken);

        var items = projected.Items
            .Select(row => new TripListItem(
                row.Id,
                row.Source,
                row.TripNumber,
                row.SequenceYear,
                row.SequenceMonth,
                row.Title,
                row.DeliverymanId,
                row.DeliverymanName,
                row.FromType,
                row.FromName,
                row.ToType,
                row.ToName,
                row.ScheduledAtUtc,
                row.Status,
                row.Source == TripSource.OrderBound
                    ? MapOrderBoundDisplayState(row.Status)
                    : ComputeDisplayState(row.Status, row.HasOpenStarted, row.HasOpenUnstarted),
                row.LatestAttemptNumber,
                row.AttemptCount,
                row.HasFailedAttempt,
                row.CreatedAt,
                row.Version,
                row.Source == TripSource.Standalone
                    && row.Status == WorkRecordStatus.Pending
                    && row.StartedAtUtc == null
                    && row.AttemptCount == 0,
                row.OrderId,
                row.OrderNumber))
            .ToList();

        return new PagedResult<TripListItem>(
            items,
            projected.TotalCount,
            projected.PageNumber,
            projected.PageSize);
    }

    private static TripDisplayState MapOrderBoundDisplayState(WorkRecordStatus status) => status switch
    {
        WorkRecordStatus.Completed => TripDisplayState.Completed,
        WorkRecordStatus.Cancelled => TripDisplayState.Cancelled,
        WorkRecordStatus.Failed => TripDisplayState.Failed,
        _ => TripDisplayState.InProgress
    };

    public async Task<TripDetails?> GetDetailsAsync(
        int id,
        int historyPage,
        int historyPageSize,
        CancellationToken cancellationToken)
    {
        using var context = contextFactory.CreateDbContext();

        var trip = await context.StandaloneTrips
            .AsNoTracking()
            .Include(item => item.Deliveryman)
            .Include(item => item.Attempts)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (trip is null)
        {
            return null;
        }

        historyPage = Math.Max(1, historyPage);
        historyPageSize = historyPageSize is 10 or 25 or 50 ? historyPageSize : 10;

        var historyQuery = context.StandaloneTripHistoryEntries
            .AsNoTracking()
            .Where(entry => entry.TripId == id);

        var historyTotal = await historyQuery.CountAsync(cancellationToken);
        var historyItems = await historyQuery
            .OrderByDescending(entry => entry.ChangedAtUtc)
            .ThenByDescending(entry => entry.Id)
            .Skip((historyPage - 1) * historyPageSize)
            .Take(historyPageSize)
            .Select(entry => new TripHistoryEntryData(
                entry.Id,
                entry.EventType,
                entry.AttemptNumber,
                entry.PreviousStatus,
                entry.NewStatus,
                entry.PreviousDeliverymanId,
                entry.NewDeliverymanId,
                entry.PreviousDeliverymanName,
                entry.NewDeliverymanName,
                entry.PreviousTripNumber,
                entry.NewTripNumber,
                entry.Reason,
                entry.ChangedByUserId,
                entry.ChangedByUserName,
                entry.ChangedAtUtc,
                entry.Source))
            .ToListAsync(cancellationToken);

        var attempts = trip.Attempts
            .OrderBy(attempt => attempt.AttemptNumber)
            .Select(attempt => new TripAttemptData(
                attempt.AttemptNumber,
                attempt.Status,
                attempt.StartedAtUtc,
                attempt.EndedAtUtc,
                attempt.Reason,
                attempt.CreatedAtUtc))
            .ToList();

        bool hasOpenStarted = trip.Attempts.Any(attempt =>
            attempt.Status == WorkRecordStatus.Pending && attempt.StartedAtUtc != null);
        bool hasOpenUnstarted = trip.Attempts.Any(attempt =>
            attempt.Status == WorkRecordStatus.Pending && attempt.StartedAtUtc == null);

        return new TripDetails(
            trip.Id,
            trip.TripNumber,
            trip.SequenceYear,
            trip.SequenceMonth,
            trip.Title,
            trip.Description,
            trip.DeliverymanId,
            trip.Deliveryman.Name.Value,
            ToPartyData(trip.From),
            ToPartyData(trip.To),
            trip.ScheduledAtUtc,
            trip.CreatedAt,
            trip.StatusChangedAtUtc,
            trip.Status,
            ComputeDisplayState(trip.Status, hasOpenStarted, hasOpenUnstarted),
            trip.LatestAttemptNumber,
            trip.Attempts.Count,
            trip.Attempts.Any(attempt => attempt.Status == WorkRecordStatus.Failed),
            trip.Version,
            trip.IsNew,
            attempts,
            new PagedResult<TripHistoryEntryData>(
                historyItems,
                historyTotal,
                historyPage,
                historyPageSize));
    }

    public async Task<TripEditorData?> GetEditorAsync(int id, CancellationToken cancellationToken)
    {
        using var context = contextFactory.CreateDbContext();

        var trip = await context.StandaloneTrips
            .AsNoTracking()
            .Include(item => item.Deliveryman)
            .Include(item => item.Attempts)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return trip is null
            ? null
            : new TripEditorData(
                trip.Id,
                trip.Version,
                trip.IsNew,
                trip.DeliverymanId,
                trip.Deliveryman.Name.Value,
                trip.Title,
                trip.Description,
                ToPartyInput(trip.From),
                ToPartyInput(trip.To),
                trip.ScheduledAtUtc);
    }

    public async Task<DeliverymanTripsSummary> GetDeliverymanSummaryAsync(
        int deliverymanId,
        CancellationToken cancellationToken)
    {
        using var context = contextFactory.CreateDbContext();

        var query = context.StandaloneTrips
            .AsNoTracking()
            .Where(trip => trip.DeliverymanId == deliverymanId);

        int total = await query.CountAsync(cancellationToken);
        int open = await query.CountAsync(
            trip => trip.Status == WorkRecordStatus.Pending,
            cancellationToken);
        int failed = await query.CountAsync(
            trip => trip.Attempts.Any(attempt => attempt.Status == WorkRecordStatus.Failed),
            cancellationToken);
        int completed = await query.CountAsync(
            trip => trip.Status == WorkRecordStatus.Completed,
            cancellationToken);
        int cancelled = await query.CountAsync(
            trip => trip.Status == WorkRecordStatus.Cancelled,
            cancellationToken);

        return new DeliverymanTripsSummary(open, failed, completed, cancelled, total);
    }

    public async Task<TripsStatistics> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        using var context = contextFactory.CreateDbContext();

        var standalone = context.StandaloneTrips.AsNoTracking();
        var orderBound = context.DeliveryTrips.AsNoTracking()
            .Where(trip => trip.OrderId != null || trip.DeliveryStepId != null);

        int total = await standalone.CountAsync(cancellationToken);
        int active = await standalone.CountAsync(
            trip => trip.Status == WorkRecordStatus.Pending,
            cancellationToken);
        int completed = await standalone.CountAsync(
            trip => trip.Status == WorkRecordStatus.Completed,
            cancellationToken);
        int failed = await standalone.CountAsync(
            trip => trip.Status == WorkRecordStatus.Failed,
            cancellationToken);
        int cancelled = await standalone.CountAsync(
            trip => trip.Status == WorkRecordStatus.Cancelled,
            cancellationToken);

        total += await orderBound.CountAsync(cancellationToken);
        active += await orderBound.CountAsync(
            trip => trip.Status == WorkRecordStatus.Pending,
            cancellationToken);
        completed += await orderBound.CountAsync(
            trip => trip.Status == WorkRecordStatus.Completed,
            cancellationToken);
        failed += await orderBound.CountAsync(
            trip => trip.Status == WorkRecordStatus.Failed,
            cancellationToken);
        cancelled += await orderBound.CountAsync(
            trip => trip.Status == WorkRecordStatus.Cancelled,
            cancellationToken);

        return new TripsStatistics(total, active, completed, failed, cancelled);
    }

    public async Task<PagedResult<DeliverymanTripListItem>> GetDeliverymanStandaloneTripsPageAsync(
        int deliverymanId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        using var context = contextFactory.CreateDbContext();

        page = Math.Max(1, page);
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var projected = await context.StandaloneTrips
            .AsNoTracking()
            .Where(trip => trip.DeliverymanId == deliverymanId)
            .OrderByDescending(trip => trip.CreatedAt)
            .ThenByDescending(trip => trip.Id)
            .Select(trip => new
            {
                trip.Id,
                trip.TripNumber,
                trip.SequenceYear,
                trip.SequenceMonth,
                trip.Title,
                trip.From.PartyType,
                FromName = trip.From.Name,
                ToType = trip.To.PartyType,
                ToName = trip.To.Name,
                trip.Status,
                trip.StartedAtUtc,
                trip.ScheduledAtUtc,
                trip.CreatedAt,
                trip.Version,
                AttemptCount = trip.Attempts.Count,
                LatestAttemptNumber = trip.Attempts.Any()
                    ? trip.Attempts.Max(attempt => attempt.AttemptNumber)
                    : (int?)null,
                HasOpenStarted = trip.Attempts.Any(attempt =>
                    attempt.Status == WorkRecordStatus.Pending && attempt.StartedAtUtc != null),
                HasOpenUnstarted = trip.Attempts.Any(attempt =>
                    attempt.Status == WorkRecordStatus.Pending && attempt.StartedAtUtc == null),
                HasFailedAttempt = trip.Attempts.Any(attempt => attempt.Status == WorkRecordStatus.Failed)
            })
            .ToPagedResultAsync(page, pageSize, cancellationToken);

        var items = projected.Items
            .Select(trip => new DeliverymanTripListItem(
                trip.Id,
                trip.TripNumber,
                trip.SequenceYear,
                trip.SequenceMonth,
                trip.Title,
                trip.PartyType,
                trip.FromName,
                trip.ToType,
                trip.ToName,
                trip.Status,
                ComputeDisplayState(trip.Status, trip.HasOpenStarted, trip.HasOpenUnstarted),
                trip.AttemptCount,
                trip.LatestAttemptNumber,
                trip.HasFailedAttempt,
                trip.ScheduledAtUtc,
                trip.CreatedAt,
                trip.Version,
                trip.Status == WorkRecordStatus.Pending
                    && trip.StartedAtUtc == null
                    && trip.AttemptCount == 0))
            .ToList();

        return new PagedResult<DeliverymanTripListItem>(
            items,
            projected.TotalCount,
            projected.PageNumber,
            projected.PageSize);
    }

    public Task<PagedResult<OrderBoundTripListItem>> GetDeliverymanOrderBoundTripsPageAsync(
        int deliverymanId,
        int page,
        int pageSize,
        CancellationToken cancellationToken) =>
        repository.GetOrderBoundTripsPageAsync(deliverymanId, page, pageSize, cancellationToken);

    private static IQueryable<StandaloneTrip> ApplyCriteria(
        IQueryable<StandaloneTrip> query,
        TripSearchCriteria criteria)
    {
        if (criteria.DeliverymanId is > 0)
        {
            int deliverymanId = criteria.DeliverymanId.Value;
            query = query.Where(trip => trip.DeliverymanId == deliverymanId);
        }

        if (criteria.PartyType is { } partyType)
        {
            query = query.Where(trip =>
                trip.From.PartyType == partyType || trip.To.PartyType == partyType);
        }

        var states = criteria.DisplayStates?
            .Where(state => Enum.IsDefined(state))
            .ToHashSet();

        if (states is { Count: > 0 })
        {
            bool includeNew = states.Contains(TripDisplayState.New);
            bool includeInProgress = states.Contains(TripDisplayState.InProgress);
            bool includeAwaitingRetry = states.Contains(TripDisplayState.AwaitingRetryStart);
            bool includeCompleted = states.Contains(TripDisplayState.Completed);
            bool includeFailed = states.Contains(TripDisplayState.Failed);
            bool includeCancelled = states.Contains(TripDisplayState.Cancelled);

            query = query.Where(trip =>
                (includeNew
                    && trip.Status == WorkRecordStatus.Pending
                    && trip.StartedAtUtc == null
                    && !trip.Attempts.Any())
                || (includeInProgress
                    && trip.Status == WorkRecordStatus.Pending
                    && trip.Attempts.Any(attempt =>
                        attempt.Status == WorkRecordStatus.Pending && attempt.StartedAtUtc != null))
                || (includeAwaitingRetry
                    && trip.Status == WorkRecordStatus.Pending
                    && trip.Attempts.Any(attempt =>
                        attempt.Status == WorkRecordStatus.Pending && attempt.StartedAtUtc == null))
                || (includeCompleted && trip.Status == WorkRecordStatus.Completed)
                || (includeCancelled && trip.Status == WorkRecordStatus.Cancelled)
                || (includeFailed
                    && (trip.Status == WorkRecordStatus.Failed
                        || trip.Attempts.Any(attempt => attempt.Status == WorkRecordStatus.Failed))));
        }

        if (criteria.ScheduledFrom is { } scheduledFrom)
        {
            DateTime fromUtc = RiyadhDayStartUtc(scheduledFrom);
            query = query.Where(trip =>
                trip.ScheduledAtUtc != null && trip.ScheduledAtUtc >= fromUtc);
        }

        if (criteria.ScheduledTo is { } scheduledTo)
        {
            DateTime toExclusiveUtc = RiyadhDayStartUtc(scheduledTo.AddDays(1));
            query = query.Where(trip =>
                trip.ScheduledAtUtc != null && trip.ScheduledAtUtc < toExclusiveUtc);
        }

        if (criteria.CreatedFrom is { } createdFrom)
        {
            DateTime fromUtc = RiyadhDayStartUtc(createdFrom);
            query = query.Where(trip => trip.CreatedAt >= fromUtc);
        }

        if (criteria.CreatedTo is { } createdTo)
        {
            DateTime toExclusiveUtc = RiyadhDayStartUtc(createdTo.AddDays(1));
            query = query.Where(trip => trip.CreatedAt < toExclusiveUtc);
        }

        string? search = criteria.Search?.Trim();
        if (string.IsNullOrEmpty(search))
        {
            return query;
        }

        string lower = search.ToLowerInvariant();
        string digits = new(search.Where(char.IsDigit).ToArray());
        long? number = long.TryParse(
            search.TrimStart('#'),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out long parsedNumber)
            ? parsedNumber
            : null;

        query = query.Where(trip =>
            trip.Title.ToLower().Contains(lower)
            || (trip.Description != null && trip.Description.ToLower().Contains(lower))
            || trip.Deliveryman.Name.Value.ToLower().Contains(lower)
            || trip.From.Name.ToLower().Contains(lower)
            || trip.To.Name.ToLower().Contains(lower)
            || (number != null && trip.TripNumber == number)
            || (trip.From.Phone != null
                && (trip.From.Phone.Contains(search)
                    || (digits.Length > 0
                        && trip.From.Phone.Replace(" ", "").Replace("-", "").Replace("+", "").Contains(digits))))
            || (trip.To.Phone != null
                && (trip.To.Phone.Contains(search)
                    || (digits.Length > 0
                        && trip.To.Phone.Replace(" ", "").Replace("-", "").Replace("+", "").Contains(digits)))));

        return query;
    }

    private static DateTime RiyadhDayStartUtc(DateOnly date) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified),
            RiyadhTimeZone);

    private static TripDisplayState ComputeDisplayState(
        WorkRecordStatus status,
        bool hasOpenStarted,
        bool hasOpenUnstarted)
    {
        if (status == WorkRecordStatus.Completed)
        {
            return TripDisplayState.Completed;
        }

        if (status == WorkRecordStatus.Cancelled)
        {
            return TripDisplayState.Cancelled;
        }

        if (status == WorkRecordStatus.Failed)
        {
            return TripDisplayState.Failed;
        }

        if (hasOpenUnstarted)
        {
            return TripDisplayState.AwaitingRetryStart;
        }

        return hasOpenStarted ? TripDisplayState.InProgress : TripDisplayState.New;
    }

    private static TripPartyData ToPartyData(TripParty party) =>
        new(party.PartyType, party.PartyId, party.Name, party.Phone, party.Address, party.Location);

    private static TripPartyInput ToPartyInput(TripParty party) =>
        new(party.PartyType, party.PartyId, party.Name, party.Phone, party.Address, party.Location);
}
