using OrderHub.Domain.Models;
using System;

namespace OrderHub.Tests.Data;

public class SupplierBuilder
{
    private string _name = "Default Supplier";
    private Address _address = Address.Create("123 Supplier St", new City("Cairo")).Value;
    private Phone _phone = Phone.Create("0123456789", "+20").Value;
    private TimeOnly _openAt = new TimeOnly(9, 0);   // 09:00 AM
    private TimeOnly _closeAt = new TimeOnly(17, 0); // 05:00 PM

    public SupplierBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public SupplierBuilder WithAddress(Address address)
    {
        _address = address;
        return this;
    }

    public SupplierBuilder WithPhone(Phone phone)
    {
        _phone = phone;
        return this;
    }

    public SupplierBuilder WithBusinessHours(TimeOnly openAt, TimeOnly closeAt)
    {
        _openAt = openAt;
        _closeAt = closeAt;
        return this;
    }

    public Supplier Build()
    {
        return new Supplier(_name, _openAt, _closeAt, _address, _phone);
    }
}