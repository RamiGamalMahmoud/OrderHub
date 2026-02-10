using System.Collections.Generic;

namespace OrderHub.Application.DTOs;

public static class CategoryDtos
{
    public record CategoryUpdateDto;
    public record CategoryCreateDto(string Name, int? ParentId);
    public record CategoryTreeDto(int Id, string Name, bool IsRootCategory, IEnumerable<CategoryTreeDto> SubCategories);
}
