using System;

namespace OrderHub.Domain.ValueObjects;

public record BusinessHours
{
    public TimeOnly OpenAt { get; }
    public TimeOnly CloseAt { get; }
    public BusinessHours(TimeOnly openAt, TimeOnly closeAt)
    {
        if (openAt > closeAt)
            throw new ArgumentException("Closing time must be after opening time.");

        OpenAt = openAt;
        CloseAt = closeAt;
    }
}
