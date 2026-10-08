

namespace OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;

public record ShippingCarrierListItem(int Id, string Name, decimal ShippingCost, string PhoneNumber, string Address);
