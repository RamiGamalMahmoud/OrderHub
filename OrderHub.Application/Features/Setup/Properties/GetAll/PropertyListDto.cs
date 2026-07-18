using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Setup.Properties.GetAll;

public record PropertyListDto(int Id, string Name, PropertyType Type);
