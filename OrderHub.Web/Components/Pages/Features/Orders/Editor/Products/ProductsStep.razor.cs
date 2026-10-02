using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderHub.Application.Features.Orders;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Web.Components.Pages.Features.Orders.Editor.State;
using static OrderHub.Web.Components.Pages.Features.Orders.Editor.Products.AddProductDialog;

namespace OrderHub.Web.Components.Pages.Features.Orders.Editor.Products;

public partial class ProductsStep
{
    [Inject]
    private IRequestExecutor RequestExecutor { get; set; } = default!;

    [Inject]
    private IDialogService DialogService { get; set; } = default!;

    [Inject]
    private ILogger<ProductsStep> Logger { get; set; } = default!;

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
                (Func<AddProductResult, Task<Result>>)OnProductAddedAsync
            }
        };

        await DialogService.ShowAsync<AddProductDialog>(
            "إضافة منتج",
            parameters,
            options);
    }

    private async Task<Result> OnProductAddedAsync(AddProductResult result)
    {
        GetOrderItemEditor.OrderItemEditorDto product;
        try
        {
            product = await RequestExecutor.ExecuteAsync(
                new GetOrderItemEditor.Query(result.Product.Id));
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to load product {ProductId} for addition.", result.Product.Id);
            return Result.Failure("تعذر تحميل بيانات المنتج. لم تتم الإضافة؛ حاول مرة أخرى.");
        }

        if (product is null)
            return Result.Failure("المنتج لم يعد متاحاً. اختر منتجاً آخر.");

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
        await InvokeAsync(StateHasChanged);
        return Result.Success();
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
