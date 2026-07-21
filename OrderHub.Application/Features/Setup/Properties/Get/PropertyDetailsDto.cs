using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Properties.Get;

public sealed record PropertyDetailsDto(
    int Id,
    string Name,
    PropertyType PropertyType,
    string Description,
    IReadOnlyCollection<PropertyOptionDto> Options);