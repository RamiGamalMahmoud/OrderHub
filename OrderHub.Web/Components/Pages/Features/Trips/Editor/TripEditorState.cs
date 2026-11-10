#nullable enable

using System;
using System.Globalization;
using OrderHub.Application.Features.Trips.Contracts;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;

namespace OrderHub.Web.Components.Pages.Features.Trips.Editor;

public sealed class TripEditorState
{
    public int TripId { get; init; }

    public int Version { get; init; }

    public bool IsEdit { get; init; }

    public int OriginalDeliverymanId { get; init; }

    public int? DeliverymanId { get; set; }

    public string? DeliverymanName { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? ScheduledDate { get; set; }

    public string? ScheduledTime { get; set; }

    public string? EditNote { get; set; }

    public string? ReassignReason { get; set; }

    public TripPartyDraft From { get; } = new();

    public TripPartyDraft To { get; } = new();

    public bool IsReassignment => IsEdit
                                  && DeliverymanId.HasValue
                                  && DeliverymanId.Value != OriginalDeliverymanId;

    public bool IsValid => Validate().IsSuccess;

    public DateTimeOffset? ScheduledAtUtc => TripSchedule.ToUtc(ScheduledDate, ScheduledTime, out _);

    public Result Validate()
    {
        if (DeliverymanId is not > 0)
        {
            return Result.Failure("المندوب مطلوب.");
        }

        TripSchedule.ToUtc(ScheduledDate, ScheduledTime, out string? scheduleError);
        if (scheduleError is not null)
        {
            return Result.Failure(scheduleError);
        }

        TripPartyInput from = From.ToInput();
        TripPartyInput to = To.ToInput();

        return IsEdit
            ? TripInputValidation.ValidateUpdate(
                Title,
                Description,
                from,
                to,
                OriginalDeliverymanId,
                DeliverymanId.Value,
                IsReassignment ? null : EditNote,
                IsReassignment ? ReassignReason : null)
            : TripInputValidation.ValidateCreate(Title, Description, from, to);
    }

    public static TripEditorState ForCreate() => new();

    public static TripEditorState ForEdit(TripEditorData data)
    {
        var (date, time) = TripSchedule.Split(data.ScheduledAtUtc);

        var state = new TripEditorState
        {
            TripId = data.Id,
            Version = data.Version,
            IsEdit = true,
            OriginalDeliverymanId = data.DeliverymanId,
            DeliverymanId = data.DeliverymanId,
            DeliverymanName = data.DeliverymanName,
            Title = data.Title,
            Description = data.Description,
            ScheduledDate = date,
            ScheduledTime = time
        };

        state.From.Load(data.From);
        state.To.Load(data.To);

        return state;
    }
}

public sealed class TripPartyDraft
{
    public TripPartyType PartyType { get; set; } = TripPartyType.Client;

    public int? PartyId { get; set; }

    public string? Name { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? Location { get; set; }

    public TripPartyInput ToInput() => PartyType == TripPartyType.External
        ? new TripPartyInput(
            PartyType,
            null,
            Trim(Name),
            Trim(Phone),
            Trim(Address),
            Trim(Location))
        : new TripPartyInput(PartyType, PartyId, null, null, null, null);

    public void Load(TripPartyInput input)
    {
        PartyType = input.PartyType;
        PartyId = input.PartyId;
        Name = input.Name;
        Phone = input.Phone;
        Address = input.Address;
        Location = input.Location;
    }

    public void ChangeType(TripPartyType type)
    {
        PartyType = type;
        PartyId = null;
        Name = null;
        Phone = null;
        Address = null;
        Location = null;
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>Converts between Riyadh wall-clock date/time input and the stored UTC instant.</summary>
public static class TripSchedule
{
    private static readonly TimeZoneInfo RiyadhTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Riyadh");

    private static readonly string[] TimeFormats = ["HH:mm", "HH:mm:ss"];

    public static DateTimeOffset? ToUtc(string? date, string? time, out string? error)
    {
        error = null;

        bool hasDate = !string.IsNullOrWhiteSpace(date);
        bool hasTime = !string.IsNullOrWhiteSpace(time);

        if (!hasDate && !hasTime)
        {
            return null;
        }

        if (!hasDate || !hasTime)
        {
            error = "أدخل تاريخ ووقت الجدولة معاً أو اتركهما فارغين.";
            return null;
        }

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate)
            || !TimeOnly.TryParseExact(time, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime))
        {
            error = "تاريخ أو وقت الجدولة غير صالح.";
            return null;
        }

        DateTime local = DateTime.SpecifyKind(parsedDate.ToDateTime(parsedTime), DateTimeKind.Unspecified);
        DateTime utc = TimeZoneInfo.ConvertTimeToUtc(local, RiyadhTimeZone);

        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    public static (string? Date, string? Time) Split(DateTime? utc)
    {
        if (utc is not { } value)
        {
            return (null, null);
        }

        DateTime local = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(value, DateTimeKind.Utc),
            RiyadhTimeZone);

        return (local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                local.ToString("HH:mm", CultureInfo.InvariantCulture));
    }
}
