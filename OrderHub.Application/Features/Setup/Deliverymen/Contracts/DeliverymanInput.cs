

namespace OrderHub.Application.Features.Setup.Deliverymen.Contracts;

public record DeliverymanInput(
        string Name,
        int CityId,
        string PhoneNumber);
