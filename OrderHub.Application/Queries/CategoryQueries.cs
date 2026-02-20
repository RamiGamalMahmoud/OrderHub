using MediatR;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.CategoryDtos;

namespace OrderHub.Application.Queries;

public static class CategoryQueries
{
    public record GetCategoryTreeQuery : IRequest<IEnumerable<CategoryTreeDto>>;
    public record GetCategoryListQuery(int? ParentId) : IRequest<IEnumerable<CategoryListDto>>;
    public record GetCategoryForEditQuery(int Id) :IRequest<CategoryEditDto>;
}
