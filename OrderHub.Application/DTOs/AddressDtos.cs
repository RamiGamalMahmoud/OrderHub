using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.DTOs
{
    public static class AddressDtos
    {
        public record AddressListDto(int Id, CityListItem CityListItem);
    }
}
