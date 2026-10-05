using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Setup.Properties.GetAll;

public record PropertyListItem(int Id, string Name, string Description, PropertyType Type);
