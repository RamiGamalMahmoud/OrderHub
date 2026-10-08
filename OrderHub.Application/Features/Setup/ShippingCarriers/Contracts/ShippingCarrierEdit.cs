

namespace OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

public record ShippingCarrierEdit(int Id, string Name, decimal ShippingCost, string CountryCode, string PhoneNumber, int CityId, string Street);
