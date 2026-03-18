using OrderHub.Domain.Models;

namespace OrderHub.Tests.Data;

public class AddressBuilder
{
    private City _city = new City("Test City");
    private string _street = "Test Street";

    public AddressBuilder WithCity(City city)
    {
        _city = city;
        return this;
    }

    public AddressBuilder WithStreet(string street)
    {
        _street = street;
        return this;
    }

    public Address Build()
    {
        return Address.Create(_street, _city).Value;
    }

    public static Address Default()
    {
        return new AddressBuilder().Build();
    }
}