namespace OrderHub.Domain.Models;

public class PropertyOption : ModelBase
{
    public int PropertyId { get; private set; }

    public string Value { get; private set; } = null!;

    public Property Property { get; private set; } = null!;

    private PropertyOption()
    {
    }

    internal static PropertyOption Create(string value)
    {
        return new PropertyOption
        {
            Value = value
        };
    }

    public void Rename(string value)
    {
        Value = value;
    }
}