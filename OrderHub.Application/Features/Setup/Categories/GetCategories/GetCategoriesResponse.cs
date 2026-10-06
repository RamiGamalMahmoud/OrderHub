using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Categories;

public sealed record GetCategoriesResponse(bool ParentExists, IReadOnlyList<CategoryIndexItem> Categories,
    IReadOnlyList<CategoryPathSegment> Path);
