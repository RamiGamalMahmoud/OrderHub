using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Application.Features.Setup.Properties.Update;

public record PropertyUpdateDto(int Id, string Name, string Description, PropertyType PropertyType, IEnumerable<PropertyOptionUpdateDto> Options);
