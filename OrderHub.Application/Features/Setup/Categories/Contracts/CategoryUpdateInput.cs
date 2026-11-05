using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public record CategoryUpdateInput(
    int Id,
    string Name,
    int? ParentId,
    string Description = null,
    CategoryColor Color = CategoryColor.Blue);
