namespace OrderHub.Domain.Enums;

public enum StandaloneTripEventType
{
    Created = 1,
    AttemptStarted = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5,
    Retried = 6,
    Reassigned = 7,
    Edited = 8
}
