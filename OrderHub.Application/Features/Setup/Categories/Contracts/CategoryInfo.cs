

namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public record CategoryInfo(
    int Id,
    string Name,
    string FullPath,
    bool HasSubCategories,
    int? ParentId = null,
    string Description = null,
    string Color = "#2563EB");
