namespace OrderHub.Application.Features.Setup.Categories;

public sealed record CategoryIndexItem(
    int Id,
    string Name,
    int DirectChildrenCount,
    int TotalProductsCount,
    string Description = null,
    string Color = "#2563EB")
{
    public bool HasChildren => DirectChildrenCount > 0;
}
