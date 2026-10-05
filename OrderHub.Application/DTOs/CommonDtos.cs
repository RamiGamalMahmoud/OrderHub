using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.DTOs;

public static class CommonDtos
{
    public record AddressInfoDto(int Id, string Street, CityInfo CityInfoDto);
    public record DeliveryMethodInfoDto(int Id, string MethodName);
}
