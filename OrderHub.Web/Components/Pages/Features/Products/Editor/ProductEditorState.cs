using OrderHub.Application.Features.Products.Create;
using OrderHub.Application.Features.Products.Update;
using OrderHub.Application.Features.Products.Editor;
using OrderHub.Domain.ValueObjects;

namespace OrderHub.Web.Components.Pages.Features.Products.Editor;

public sealed class ProductEditorState
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public decimal Price { get; set; }
    public int? CategoryId { get; set; }
    public string CategoryPath { get; set; } = "";
    public HashSet<int> SupplierIds { get; } = [];
    public List<ProductPropertyEditorState> Properties { get; } = [];

    public static ProductEditorState FromRead(ProductEditorData data)
    {
        var state = new ProductEditorState();
        if (data.Product is { } product)
        {
            state.Name = product.Name;
            state.Code = product.Code;
            state.Price = product.Price;
            state.CategoryId = product.CategoryId;
            state.CategoryPath = string.Join(" / ", data.SelectedCategoryPath.Select(segment => segment.Name));
            state.SupplierIds.UnionWith(product.SelectedSuppliersIds);
        }
        var assignments = data.Product?.ProductProperties.ToDictionary(item => item.PropertyId);
        foreach (var definition in data.Properties)
        {
            var assignment = assignments?.GetValueOrDefault(definition.Id);
            state.Properties.Add(new ProductPropertyEditorState
            {
                Definition = definition,
                Assigned = assignment is not null,
                Required = assignment?.isRequired ?? false
            });
        }
        return state;
    }

    public CreateProductCommand CreateCommand() => new(new ProductCreateInput(
        Name, Code, Price, CategoryId.GetValueOrDefault(), SupplierIds.ToArray(),
        Properties.Where(item => item.Assigned).Select(item => new ProductPropertyInput(item.Definition.Id, item.Required)).ToArray()));

    public UpdateProductCommand UpdateCommand(int id) => new(id, new ProductUpdateInput(
        Name, Code, Price, CategoryId.GetValueOrDefault(), SupplierIds.ToArray(),
        Properties.Where(item => item.Assigned).Select(item => new ProductPropertyUpdate(item.Definition.Id, item.Required)).ToArray()));

    public static string? ValidateName(string value)
    {
        try
        {
            _ = new EntityName(value?.Trim());
            return null;
        }
        catch (ArgumentException)
        {
            return "اسم المنتج يجب أن يتكون من 2 إلى 100 حرف.";
        }
    }

    public static string? ValidateCode(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim().Length > 10
            ? "كود المنتج مطلوب ويجب ألا يتجاوز 10 أحرف." : null;

    public static string? ValidatePrice(decimal value) =>
        value < 0 || value > 9999999999999999.99m || decimal.Round(value, 2) != value
            ? "أدخل سعرًا غير سالب لا يتجاوز منزلتين عشريتين." : null;

    public string? Validate() => ValidateName(Name) ?? ValidateCode(Code) ?? ValidatePrice(Price) ??
        (CategoryId is null or <= 0 ? "اختر قسمًا للمنتج." : null);
}

public sealed class ProductPropertyEditorState
{
    public ProductEditorProperty Definition { get; init; } = null!;
    public bool Assigned { get; set; }
    public bool Required { get; set; }
}
