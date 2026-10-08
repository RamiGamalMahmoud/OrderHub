using System;
using OrderHub.Domain.Models;

namespace OrderHub.Tests.Domain;

public class CityTests
{
    [Fact]
    public void Names_are_trimmed_when_created_and_renamed()
    {
        var city = new City("  Cairo  ");

        Assert.Equal("Cairo", city.Name.Value);

        city.Rename("  Alexandria  ");

        Assert.Equal("Alexandria", city.Name.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" a ")]
    public void Domain_rejects_invalid_names(string name)
    {
        Assert.Throws<ArgumentException>(() => new City(name));

        var city = new City("Cairo");

        Assert.Throws<ArgumentException>(() => city.Rename(name));
        Assert.Equal("Cairo", city.Name.Value);
    }
}
