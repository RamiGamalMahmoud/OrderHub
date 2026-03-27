namespace OrderHub.Application.DTOs;

public static class DeliverymanDtos
{
    public record DeliverymanListDto(int Id, string Name, string CityName, string PhoneNumber);
    public record DeliverymanFormDto(string Name, int CityId, string PhoneNumber);
}
