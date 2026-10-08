namespace OrderHub.Application.Features.Setup.Categories;

public sealed record CategoryIndexItem(int Id, string Name, int DirectChildrenCount, int TotalProductsCount)
{
    public bool HasChildren => DirectChildrenCount > 0;
}
