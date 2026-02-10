namespace OrderHub.Application.DTOs;

public static class ShippingCarriersDtos
{
    public record ShippingCarrierListDto(int Id, string Name, decimal ShippingCost, string PhoneNumber, string Address);
    public record ShippingCarrierEditDto(int Id, string Name, decimal ShippingCost, string CountryCode, string PhoneNumber, int CityId, string Street);
    public record ShippingCarrierCreateDto(string Name, decimal ShippingCost, string CountryCode, string PhoneNumber, int CityId, string Street);
    public record ShippingCarrierUpdateDto(int Id, string Name, decimal ShippingCost, string CountryCode, string PhoneNumber, int CityId, string Street);
}
