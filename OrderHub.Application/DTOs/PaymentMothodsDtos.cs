namespace OrderHub.Application.DTOs;

public static class PaymentMothodsDtos
{
    public record PaymentMethodListDto(int Id, string DisplayName, string Description, bool IsActive);
}
