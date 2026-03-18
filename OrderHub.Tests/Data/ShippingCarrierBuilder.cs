using OrderHub.Domain.Models;

namespace OrderHub.Tests.Data;

public class ShippingCarrierBuilder
{
    private string _name = "Test Carrier";
    private decimal _shippingCost = 10;
    private Phone _phone = new PhoneBuilder().Build();
    private Address _address = AddressBuilder.Default();

    public ShippingCarrierBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ShippingCarrierBuilder WithShippingCost(decimal cost)
    {
        _shippingCost = cost;
        return this;
    }

    public ShippingCarrierBuilder WithPhone(string phone)
    {
        _phone = Phone.Create(phone, "+2").Value;
        return this;
    }

    public ShippingCarrierBuilder WithoutPhone()
    {
        _phone = null;
        return this;
    }

    public ShippingCarrierBuilder WithAddress(Address address)
    {
        _address = address;
        return this;
    }

    public ShippingCarrier Build()
    {
        return new ShippingCarrier(
            _name,
            _shippingCost,
            _phone,
            _address
        );
    }
}