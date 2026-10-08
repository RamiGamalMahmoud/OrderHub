using OrderHub.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace OrderHub.Domain.Models;

public partial class Client : ModelBase
{
    private Client() { }

    public Client(string name, string street, City city, string phoneNumber, string countryCode)
    {
        UpdateName(name);
        UpdateAddress(city, street);
        UpdatePhone(countryCode, phoneNumber);
    }

    public EntityName Name { get; private set; }
    public string Location { get; private set; }
    private readonly List<Order> _orders = [];
    public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();
    public Phone Phone { get; private set; }
    public Address Address { get; private set; }

    public void UpdateLocation(string location)
    {
        Location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
    }

    public void UpdateName(string name) => Name = new EntityName(name?.Trim());

    public void UpdatePhone(string countryCode, string number)
    {
        var phone = Phone.Create(number?.Trim() ?? "", countryCode?.Trim() ?? "");
        if (!phone.IsSuccess)
        {
            throw new ArgumentException(phone.ErrorMessage);
        }

        if (Phone is null)
        {
            Phone = phone.Value;
            return;
        }

        Phone.ChangeNumber(phone.Value.Number.NationalNumber, phone.Value.Number.CountryCode);
    }

    public void UpdateAddress(City city, string street)
    {
        var address = Address.Create(street?.Trim(), city);
        if (!address.IsSuccess)
        {
            throw new ArgumentException(address.ErrorMessage);
        }

        if (Address is null)
        {
            Address = address.Value;
            return;
        }

        Address.ChangeCity(city);
        Address.ChangeStreet(address.Value.Street);
    }

    // A contact may be shared by another party; replacing it keeps that party intact.
    public void ReplacePhone(Phone phone)
    {
        ArgumentNullException.ThrowIfNull(phone);
        Phone = phone;
    }

    public void ReplaceAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        Address = address;
    }
}
