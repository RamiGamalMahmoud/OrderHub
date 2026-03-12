namespace OrderHub.Application.DTOs;

public static class PaymentMothodsDtos
{
    public record PaymentMethodListDto(int Id, string DisplayName, string Description, bool IsActive);
    public record PaymentMethodEditDto(int Id, string DisplayName, string Description, bool IsActive);
    public record PaymentMethodCreateDto(string DisplayName, string Description, bool IsActive);
}
