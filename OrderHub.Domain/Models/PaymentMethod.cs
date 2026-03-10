namespace OrderHub.Domain.Models;

public class PaymentMethod : ModelBase
{
    public string Code { get; set; }  // CASH, TRANSFER, SUPPLIER_ACCOUNT, BASKET

    public string DisplayName { get; set; }

    public string Description { get; set; }

    public bool IsActive { get; set; } = true;
}