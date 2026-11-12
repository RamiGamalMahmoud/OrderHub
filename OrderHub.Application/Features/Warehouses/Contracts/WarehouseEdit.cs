namespace OrderHub.Application.Features.Warehouses.Contracts;

public record WarehouseEdit(
    int Id,
    string Name,
    int KeeperUserId,
    bool IsActive);
