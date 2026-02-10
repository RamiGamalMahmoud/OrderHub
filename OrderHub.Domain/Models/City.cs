using OrderHub.Domain.ValueObjects;

namespace OrderHub.Domain.Models;

public class City : ModelBase
{
    private City() { }

    public City(string name)
    {
        Name = new EntityName(name);
    }

    public void Rename(string name)
    {
        Name = new EntityName(name);
    }

    public EntityName Name { get; private set; }
    public override string ToString() => Name.Value;

}
