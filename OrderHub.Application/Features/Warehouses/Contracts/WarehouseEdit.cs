namespace OrderHub.Application.Features.Warehouses.Contracts;

public record WarehouseEdit(
    int Id,
    string Name,
    int KeeperUserId,
    string KeeperDisplayName,
    bool IsActive,
    int? CityId,
    string Street,
    string PhoneNumber,
    string CountryCode);
