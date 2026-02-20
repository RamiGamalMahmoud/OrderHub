using System.Collections.Generic;

namespace OrderHub.Application.DTOs;

public static class CategoryDtos
{
    public record CategoryUpdateDto(int Id, string Name, int? ParentId);
    public record CategoryCreateDto(string Name, int? ParentId);
    public record CategoryTreeDto(int Id, string Name, bool IsRootCategory, IEnumerable<CategoryTreeDto> SubCategories);
    public record CategoryListDto(int Id, string Name, bool IsRootCategory, int SubCategoriesCount, int ProductsCount, int? ParentId)
    {
        public bool HasSubCategories => SubCategoriesCount > 0;
    }
    public record CategoryEditDto(int Id, string Name, int? ParentId);
}
