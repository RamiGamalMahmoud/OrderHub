using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;

namespace OrderHub.Tests.Data;

public class DeliverymanBuilder
{
    private string _name = "John Doe";
    private City _city = new City("Cairo");
    private string _phoneNumber = "0123456789";

    public DeliverymanBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public DeliverymanBuilder WithCity(City city)
    {
        _city = city;
        return this;
    }

    public DeliverymanBuilder WithPhoneNumber(string phoneNumber)
    {
        _phoneNumber = phoneNumber;
        return this;
    }

    public Deliveryman Build()
    {
        return new Deliveryman(_name, _city, _phoneNumber);
    }
}
