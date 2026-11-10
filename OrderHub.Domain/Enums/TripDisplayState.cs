namespace OrderHub.Domain.Enums;

public enum TripDisplayState
{
    New = 1,
    InProgress = 2,
    AwaitingRetryStart = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}
