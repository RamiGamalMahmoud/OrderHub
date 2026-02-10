using OrderHub.Domain.ValueObjects;
using System;

namespace OrderHub.Domain.Models;

public class Supplier : ModelBase
{
    private Supplier() { }

    public Supplier(string name, TimeOnly openAt, TimeOnly closeAt, Address address, Phone phone)
    {
        UpdateName(name);
        UpdateAddress(address);
        UpdatePhone(phone);
        UpdateBuisnessHours(openAt, closeAt);
    }

    public Supplier(string name)
    {
        UpdateName(name);
    }

    public EntityName Name { get; private set; }

    public Phone Phone { get; private set; }

    public Address Address { get; private set; }

    public BusinessHours BusinessHours { get; private set; }

    public void UpdatePhone(Phone phone)
    {
        if (phone is not null)
        {
            Phone = phone;
        }
    }

    public void UpdateName(string name)
    {
        Name = new EntityName(name);
    }

    public void UpdateAddress(Address address)
    {
        if (address is not null)
        {
            Address = address;
        }
    }

    public void UpdateBuisnessHours(TimeOnly openAt, TimeOnly closeAt)
    {
        BusinessHours = new BusinessHours(openAt, closeAt);
    }
}
