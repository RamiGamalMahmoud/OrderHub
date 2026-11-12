namespace OrderHub.Application.Features.Warehouses.GetKeeperCandidates;

public sealed record WarehouseKeeperCandidate(
    int Id,
    string DisplayName,
    bool IsActive,
    bool HasInventoryWrite);
