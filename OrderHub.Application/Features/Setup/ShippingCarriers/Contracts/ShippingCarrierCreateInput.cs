

namespace OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

public record ShippingCarrierCreateInput(string Name, decimal ShippingCost, string CountryCode, string PhoneNumber, int CityId, string Street);
