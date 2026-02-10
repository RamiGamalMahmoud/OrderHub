using MediatR;
using System.Collections.Generic;
using static OrderHub.Application.DTOs.CategoryDtos;

namespace OrderHub.Application.Queries;

public static class CategoryQueries
{
    public record GetCategoryTreeQuery : IRequest<IEnumerable<CategoryTreeDto>>;
}
