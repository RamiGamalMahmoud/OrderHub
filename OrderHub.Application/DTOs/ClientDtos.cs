namespace OrderHub.Application.DTOs;

public static class ClientDtos
{
    public record ClientListDto(int Id, string Name, string Address, string PhoneNumber, string Location);
    public record ClientFormDto(string Name, string Street, int CityId, string PhoneNumber, string CountryCode, string Location);
}
