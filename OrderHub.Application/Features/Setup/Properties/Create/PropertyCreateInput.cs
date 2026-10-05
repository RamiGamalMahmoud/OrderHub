using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Properties.Create;

public sealed record PropertyCreateInput(string Name, PropertyType PropertyType, string Description, IEnumerable<PropertyOptionInput> PropertyOptions);
