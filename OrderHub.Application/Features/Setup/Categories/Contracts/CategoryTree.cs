using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public record CategoryTree(int Id, string Name, bool IsRootCategory, IEnumerable<CategoryTree> SubCategories);
