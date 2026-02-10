namespace OrderHub.Application.DTOs;

public static class DeliverymanDtos
{
    public record DeliverymanUpdateDto(int Id, string Name, int CityId);
    public record DeliverymanCreateDto(string Name, int CityId);
    public record DeliverymanListDto(int Id, string Name, string CityName);
    public record DeliverymanEditDto(int Id, string Name, int CityId);
}
