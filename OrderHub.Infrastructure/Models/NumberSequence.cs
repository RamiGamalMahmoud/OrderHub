using OrderHub.Domain.Enums;
using System;

namespace OrderHub.Infrastructure.Models;

internal class NumberSequence
{
    private NumberSequence()
    {
    }

    private NumberSequence(
        NumberSequenceKind kind,
        int partyId,
        int year,
        int month)
    {
        Kind = kind;
        PartyId = partyId;
        Year = year;
        Month = month;
    }

    public NumberSequenceKind Kind { get; private set; }
    public int PartyId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public long LastNumber { get; private set; }

    public static NumberSequence CreateGlobal(NumberSequenceKind kind)
    {
        ValidateKind(kind);
        return new NumberSequence(kind, 0, 0, 0);
    }

    public static NumberSequence CreateMonthly(
        NumberSequenceKind kind,
        int partyId,
        int year,
        int month)
    {
        ValidateKind(kind);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(partyId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(year);

        if (month is < 1 or > 12)
            throw new ArgumentOutOfRangeException(nameof(month));

        return new NumberSequence(kind, partyId, year, month);
    }

    private static void ValidateKind(NumberSequenceKind kind)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
    }
}
