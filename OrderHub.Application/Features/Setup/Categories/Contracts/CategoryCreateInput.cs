namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public record CategoryCreateInput(
    string Name,
    int? ParentId,
    string Description = null,
    string Color = "#2563EB");
