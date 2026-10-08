namespace OrderHub.Domain.Enums;

public enum OrderHistorySource
{
    Creation,
    ManualAction,
    Retry,
    LastStepCompleted,
    DirectDelivery,
    OrderCancellation,
    LegacyMigration
}
