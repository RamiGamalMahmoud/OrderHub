namespace OrderHub.Application.Common.Lookups;

public record CategoryLookup(int Id, string Name, string FullPath, bool HasSubCategories, int? ParentId = null);
