using OrderHub.Domain.Enums;
using System.Collections.Generic;

namespace OrderHub.Domain.Models;

public class Property : ModelBase
{
    public string Name { get; set; }
    public PropertyType PropertyType { get; set; }
    public ICollection<PropertyOption> Options { get; set; } = new List<PropertyOption>();
}
