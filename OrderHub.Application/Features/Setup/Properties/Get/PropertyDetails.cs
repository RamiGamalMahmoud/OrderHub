using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Properties.Get;

public sealed record PropertyDetails(
    int Id,
    string Name,
    PropertyType PropertyType,
    string Description,
    IReadOnlyCollection<PropertyOptionItem> Options);
