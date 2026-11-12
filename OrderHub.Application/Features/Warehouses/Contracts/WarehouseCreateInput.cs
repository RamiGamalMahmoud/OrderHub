namespace OrderHub.Application.Features.Warehouses.Contracts;

public record WarehouseCreateInput(
    string Name,
    int KeeperUserId,
    int CityId,
    string Street,
    string PhoneNumber,
    string CountryCode);
