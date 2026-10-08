using OrderHub.Domain.ValueObjects;

namespace OrderHub.Domain.Models;

public class City : ModelBase
{
    private City()
    {
    }

    public City(string name)
    {
        Rename(name);
    }

    public void Rename(string name)
    {
        Name = new EntityName(name?.Trim());
    }

    public EntityName Name { get; private set; }

    public override string ToString() => Name.Value;
}
