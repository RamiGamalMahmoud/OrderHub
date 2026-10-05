using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Categories.Contracts;

public record CategoryEdit(int Id, string Name, int? ParentId);
