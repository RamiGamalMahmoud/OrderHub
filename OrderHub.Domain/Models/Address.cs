using OrderHub.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderHub.Domain.Models;

public partial class Address : ModelBase
{
    private Address() { }

    private Address(string street, City city, bool isPrimary = false)
    {
        ChangeStreet(street);
        ChangeCity(city);
        IsPrimary = isPrimary;
    }

    public static Result<Address> Create(string street, City city)
    {
        List<string> errors = [];
        if (string.IsNullOrWhiteSpace(street))
            errors.Add("Street name cannot be empty");

        if(city is null)
            errors.Add("City cannot be null");

        if(errors.Any())
        {
            return Result<Address>.Failure(string.Join(", ", errors));
        }

        return Result<Address>.Success(new Address(street, city));
    }

    public string Street { get; private set; }
    public City City { get; private set; }
    public string FullAddress => $"{City?.Name} - {Street}";
    public bool IsPrimary { get; private set; }

    public void ChangeCity(City city)
    {
        if (city is null)
            throw new ArgumentNullException("City cannot be null");
        City = city;
    }

    public void ChangeStreet(string street)
    {
        if (string.IsNullOrWhiteSpace(street))
            throw new ArgumentException("Street name cannot be empty");
        Street = street;
    }

    public override string ToString() => $"{Street}, {City}";

}
