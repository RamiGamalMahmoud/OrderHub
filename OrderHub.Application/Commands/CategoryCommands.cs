using MediatR;
using OrderHub.Domain.Common;
using static OrderHub.Application.DTOs.CategoryDtos;

namespace OrderHub.Application.Commands;

public static class CategoryCommands
{
    public record CreateCategoryCommand(CategoryCreateDto CategoryCreateDto) : IRequest<Result>;
    public record DeleteCategoryCommand(int Id) : IRequest<Result>;
    public record UpdateCategoryCommand(CategoryUpdateDto CategoryUpdateDto) : IRequest<Result>;
}
