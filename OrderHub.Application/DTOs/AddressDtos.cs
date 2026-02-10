using static OrderHub.Application.DTOs.CityDtos;

namespace OrderHub.Application.DTOs
{
    public static class AddressDtos
    {
        public record AddressListDto(int Id, CityListDto CityListDto);
    }
}
