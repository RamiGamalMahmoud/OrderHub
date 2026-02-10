namespace OrderHub.Application.DTOs;

public static class CityDtos
{
    public record CityListDto(int Id, string Name);
    public record CityCreateDto(string Name);
    public record CityUpdateDto(int Id, string Name);
}
