using OrderHub.Domain.Models;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Exceptions;

namespace OrderHub.Tests.Domain;

public class PropertyTests
{
    [Theory]
    [InlineData(PropertyType.Text)]
    [InlineData(PropertyType.Boolean)]
    [InlineData(PropertyType.List)]
    public void Actual_property_types_accept_only_their_supported_options(PropertyType type)
    {
        var property = Property.Create("Property", type,
            options: type == PropertyType.List ? ["Option"] : []);

        Assert.Equal(type, property.PropertyType);
        Assert.Equal(type == PropertyType.List ? 1 : 0, property.Options.Count);
        Assert.Throws<DomainException>(() => Property.Create("Property", (PropertyType)99));
        Assert.Throws<DomainException>(() => Property.Create("Property", PropertyType.Text, options: ["Option"]));
    }

    [Fact]
    public void List_values_are_normalized_required_bounded_and_unique()
    {
        var property = Property.Create(" Color ", PropertyType.List,
            " Description ", [" Red ", "Blue"]);

        Assert.Equal("Color", property.Name);
        Assert.Equal("Description", property.Description);
        Assert.Throws<DomainException>(() => property.AddOption(" red "));
        Assert.Throws<DomainException>(() => property.AddOption(" "));
        Assert.Throws<DomainException>(() => property.AddOption(new string('x', 101)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Required_names_are_enforced(string name)
    {
        Assert.Throws<DomainException>(() =>
            Property.Create(name, PropertyType.Text));
    }

    [Fact]
    public void Invalid_replacement_does_not_partially_change_the_aggregate()
    {
        var property = Property.Create("Original", PropertyType.List,
            "Description", ["Red", "Blue"]);

        Assert.Throws<DomainException>(() => property.UpdateDefinition("Renamed",
            PropertyType.List, "Changed", [new(null, "Same"), new(null, " same ")]));

        Assert.Equal("Original", property.Name);
        Assert.Equal("Description", property.Description);
        Assert.Equal(2, property.Options.Count);
    }
}
