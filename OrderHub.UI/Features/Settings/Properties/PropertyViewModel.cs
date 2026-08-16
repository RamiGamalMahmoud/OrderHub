using OrderHub.Application.Common.Lookups;
using OrderHub.Domain.Enums;

namespace OrderHub.UI.Features.Settings.Properties;

public class PropertyViewModel
{
    public int Id { get;  set; }
    public string Name { get;  set; } = string.Empty;
    public string Description { get;  set; }
    public EnumItem<PropertyType> Type { get;  set; }
}