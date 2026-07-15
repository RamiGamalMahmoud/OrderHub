namespace OrderHub.Domain.Models;

public class PropertyOption : ModelBase
{
    public int PropertyId { get; set; }
    public string Value { get; set; } = null;
    public Property Property { get; set; }
}
