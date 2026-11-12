namespace OrderHub.Application.Features.Warehouses.Contracts;

public record WarehouseUpdateInput(
    int Id,
    string Name,
    int KeeperUserId,
    bool IsActive,
    int CityId,
    string Street,
    string PhoneNumber,
    string CountryCode);
