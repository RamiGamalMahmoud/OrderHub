using OrderHub.Domain.ValueObjects;
using System;

namespace OrderHub.Domain.Models;

public class Deliveryman : ModelBase
{
    private Deliveryman()
    {
    }

    public Deliveryman(string name, City city, string phoneNumber)
    {
        Rename(name);
        ChangeCity(city);
        ChangePhoneNumber(phoneNumber);
    }

    public string PhoneNumber { get; private set; }

    public EntityName Name { get; private set; }

    public City City { get; private set; }

    public void Rename(string name)
    {
        Name = new EntityName(name?.Trim());
    }

    public void ChangeCity(City city)
    {
        ArgumentNullException.ThrowIfNull(city);
        City = city;
    }

    public void ChangePhoneNumber(string phoneNumber)
    {
        if (phoneNumber?.Trim().Length > 15)
        {
            throw new ArgumentException("رقم هاتف المندوب يجب ألا يتجاوز 15 حرفًا.");
        }

        PhoneNumber = phoneNumber?.Trim();
    }
}
