using OrderHub.Domain.Models;

namespace OrderHub.Tests.Data;

public class ClientBuilder
{
    private string _name = "Default Client";
    private string _street = "123 Main St";
    private City _city = new City("Cairo");
    private string _phoneNumber = "0123456789";
    private string _countryCode = "+20";

    public ClientBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ClientBuilder WithStreet(string street)
    {
        _street = street;
        return this;
    }

    public ClientBuilder WithCity(City city)
    {
        _city = city;
        return this;
    }

    public ClientBuilder WithPhoneNumber(string number, string countryCode = "+20")
    {
        _phoneNumber = number;
        _countryCode = countryCode;
        return this;
    }

    public Client Build()
    {
        return new Client(_name, _street, _city, _phoneNumber, _countryCode);
    }
}