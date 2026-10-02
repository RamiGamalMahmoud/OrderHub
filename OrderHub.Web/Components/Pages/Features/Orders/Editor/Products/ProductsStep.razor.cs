using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Interfaces;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;
using static OrderHub.Web.Components.Pages.Features.Orders.Editor.Products.AddProductDialog;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.Products;

public partial class ProductsStep
{
    [Inject]
    private IRequestExecutor RequestExecutor { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Parameter]
    [EditorRequired]
    public required List<OrderItemEditorState> Items { get; set; }

    private async Task AddProductAsync()
    {
        DialogOptions options = new()
        {
            Position = DialogPosition.TopCenter,
            CloseOnEscapeKey = true,
            CloseButton = true,
            DefaultFocus = DefaultFocus.Element
        };

        DialogParameters<AddProductDialog> parameters = new()
        {
            {
                x => x.ProductAdded,
                EventCallback.Factory.Create<AddProductResult>(
                    this,
                    OnProductAddedAsync)
            }
        };

        await DialogService.ShowAsync<AddProductDialog>(
            "إضافة منتج",
            parameters,
            options);
    }

    private async Task OnProductAddedAsync(AddProductResult result)
    {
        var product = await RequestExecutor.ExecuteAsync(
            new GetOrderItemEditor.Query(result.Product.Id));

        OrderItemEditorState item = new()
        {
            ProductId = product.ProductId,
            ProductName = product.Name,
            CategoryName = product.CategoryName,

            UnitPrice = result.UnitPrice,
            Quantity = result.Quantity,
            VatRate = result.VatRate,

            Suppliers = product.Suppliers
                .Select(supplier => new OrderItemSupplierEditorState(
                    supplier.Id,
                    supplier.Name))
                .ToList(),

            Properties = product.Properties
                .Select(property => new OrderItemPropertyEditorState
                {
                    PropertyId = property.Id,
                    Name = property.Name,
                    IsRequired = property.IsRequired,
                    Type = property.PropertyType,

                    Options = property.Options
                        .Select(option => new OrderItemPropertyOptionEditorState(
                            option.Id,
                            option.Name))
                        .ToList()
                })
                .ToList()
        };

        Items.Add(item);
    }

    private void EditItem(OrderItemEditorState item)
    {
        // سنضيف ProductEditorDrawer في الخطوة التالية.
    }

    private void RemoveItem(OrderItemEditorState item)
    {
        Items.Remove(item);
    }
}