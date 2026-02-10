namespace OrderHub.Application.DTOs;

public static class CommonDtos
{
    public record AddressInfoDto(int Id, string Street, CityInfoDto CityInfoDto);
    public record CategoryInfoDto(int Id, string Name, string FullPath, int? ParentId = null);
    public record CityInfoDto(int Id, string Name);
    public record ClientInfoDto(int Id, string Name);
    public record SupplierInfoDto(int Id, string Name);
    public record ProductInfoDto(int Id, string Name);
    public record OrderStatusInfoDto(int Id, string Status, string DisplayName);
    public record DeliveryMethodInfoDto(int Id, string MethodName);
}
