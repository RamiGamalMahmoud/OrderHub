using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Properties.Update;

public record PropertyUpdateInput(int Id, string Name, string Description, PropertyType PropertyType, IEnumerable<PropertyOptionUpdate> Options);
