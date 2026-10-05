using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public record CategoryListItem(int Id, string Name, bool IsRootCategory, int SubCategoriesCount, int ProductsCount, int? ParentId)
{
    public bool HasSubCategories => SubCategoriesCount > 0;
}
