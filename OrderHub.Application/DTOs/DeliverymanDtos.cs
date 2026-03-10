namespace OrderHub.Application.DTOs;

public static class DeliverymanDtos
{
    public record DeliverymanUpdateDto(int Id, string Name, int CityId, string PhoneNumber);
    public record DeliverymanCreateDto(string Name, int CityId, string PhoneNumber);
    public record DeliverymanListDto(int Id, string Name, string CityName, string PhoneNumber);
    public record DeliverymanEditDto(int Id, string Name, int CityId, string PhoneNumber);
}
