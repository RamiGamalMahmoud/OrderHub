using System;
using System.Globalization;

namespace OrderHub.Domain.ValueObjects;

public record Money : IComparable<Money>
{
    public decimal Value { get; }

    public Money(decimal value)
    {
        if (value < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(value));

        Value = value;
    }

    public override string ToString() => Value.ToString("0.00", CultureInfo.InvariantCulture);

    public string ToString(string format) => Value.ToString(format, CultureInfo.InvariantCulture);

    public string ToString(IFormatProvider formatProvider) => Value.ToString("0.00", formatProvider);

    public int CompareTo(Money other)
    {
        if (other is null) return 1;
        return Value.CompareTo(other.Value);
    }

    // Arithmetic operators
    public static Money operator +(Money a, Money b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        return new Money(a.Value + b.Value);
    }

    public static Money operator -(Money a, Money b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Value - b.Value < 0)
            throw new InvalidOperationException("Resulting price cannot be negative.");

        return new Money(a.Value - b.Value);
    }

    public static Money operator *(Money price, decimal multiplier)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (multiplier < 0)
            throw new ArgumentException("Multiplier cannot be negative.", nameof(multiplier));

        return new Money(price.Value * multiplier);
    }

    public static Money operator *(decimal multiplier, Money price) => price * multiplier;

    public static Money operator /(Money price, decimal divisor)
    {
        ArgumentNullException.ThrowIfNull(price);

        if (divisor <= 0)
            throw new ArgumentException("Divisor must be positive.", nameof(divisor));

        return new Money(price.Value / divisor);
    }

    // Comparison operators
    public static bool operator <(Money left, Money right)
    {
        if (left is null) return right is not null;
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(Money left, Money right)
    {
        if (left is null) return false;
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(Money left, Money right)
    {
        if (left is null) return true;
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(Money left, Money right)
    {
        if (left is null) return right is null;
        return left.CompareTo(right) >= 0;
    }

    // Factory methods
    public static Money Zero => new(0m);
}