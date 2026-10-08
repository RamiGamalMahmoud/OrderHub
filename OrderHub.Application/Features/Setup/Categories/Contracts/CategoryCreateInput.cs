using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public record CategoryCreateInput(string Name, int? ParentId);
