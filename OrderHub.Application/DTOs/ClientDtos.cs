using System;

namespace OrderHub.Application.DTOs;

public static class ClientDtos
{
    public record ClientCreateDto(string Name, string Street, int CityId, string PhoneNumber, string CountryCode);
    public record ClientListDto(int Id, string Name, string Address, string PhoneNumber);
    public record ClientUpdateDto(int Id, string Name, string Street, int CityId, string PhoneNumber, string CountryCode);
    public record ClientEditDto(int Id, string Name, string Street, int CityId, string PhoneNumber, string CountryCode);
}
