using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public record CategoryUpdateInput(int Id, string Name, int? ParentId);
