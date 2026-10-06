namespace OrderHub.Infrastructure.Features.Setup.Categories;

internal sealed record CategoryHierarchyRow(int Id, string Name, int? ParentId, int DirectProductsCount);
