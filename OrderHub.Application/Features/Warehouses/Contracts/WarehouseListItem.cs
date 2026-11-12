namespace OrderHub.Application.Features.Warehouses.Contracts;

public record WarehouseListItem(
    int Id,
    string Name,
    string KeeperDisplayName,
    bool IsActive,
    int PendingIssueOrderCount,
    string Address,
    string PhoneNumber);
