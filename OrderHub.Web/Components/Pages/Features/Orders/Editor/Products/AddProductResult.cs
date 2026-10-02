using OrderHub.Application.Features.Products.Contracts;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.Products;

public partial class AddProductDialog
{
    public sealed record AddProductResult(
        ProductLookupItem Product,
        decimal UnitPrice,
        int Quantity,
        decimal VatRate);
}
