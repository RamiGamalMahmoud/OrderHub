using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Setup.Categories;

public sealed record CategoryIndexItem(
    int Id,
    string Name,
    int DirectChildrenCount,
    int TotalProductsCount,
    string Description = null,
    CategoryColor Color = CategoryColor.Blue)
{
    public bool HasChildren => DirectChildrenCount > 0;
}
