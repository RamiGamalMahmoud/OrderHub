

namespace OrderHub.Application.Features.Setup.PaymentMethods.Contracts;

public record PaymentMethodListItem(int Id, string DisplayName, string Description, bool IsActive);
