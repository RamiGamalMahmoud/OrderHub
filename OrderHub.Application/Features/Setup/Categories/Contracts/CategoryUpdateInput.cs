namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public record CategoryUpdateInput(
    int Id,
    string Name,
    int? ParentId,
    string Description = null,
    string Color = "#2563EB");
