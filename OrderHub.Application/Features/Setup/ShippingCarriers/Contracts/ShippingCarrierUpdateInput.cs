

namespace OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

public record ShippingCarrierUpdateInput(int Id, string Name, decimal ShippingCost, string CountryCode, string PhoneNumber, int CityId, string Street);
