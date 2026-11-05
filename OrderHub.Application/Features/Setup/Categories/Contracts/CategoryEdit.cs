using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public record CategoryEdit(
    int Id,
    string Name,
    int? ParentId,
    string Description = null,
    CategoryColor Color = CategoryColor.Blue);
