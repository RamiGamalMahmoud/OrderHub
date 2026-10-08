

namespace OrderHub.Application.Features.Setup.Clients.Contracts;

public record ClientInput(string Name, string Street, int CityId, string PhoneNumber, string CountryCode, string Location);
