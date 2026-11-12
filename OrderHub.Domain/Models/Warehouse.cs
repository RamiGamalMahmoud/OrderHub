using System;
using OrderHub.Domain.ValueObjects;

namespace OrderHub.Domain.Models;

public class Warehouse : ModelBase
{
    private Warehouse() { }

    public Warehouse(
        string name,
        int keeperUserId,
        City city,
        string street,
        string phoneNumber = null,
        string countryCode = null)
    {
        Rename(name);
        AssignKeeper(keeperUserId);
        UpdateAddress(city, street);
        UpdatePhone(countryCode, phoneNumber);
        IsActive = true;
    }

    public EntityName Name { get; private set; }

    public int KeeperUserId { get; private set; }

    public User Keeper { get; private set; }

    public Address Address { get; private set; }

    public Phone Phone { get; private set; }

    public bool IsActive { get; private set; }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = new EntityName(name.Trim());
    }

    public void AssignKeeper(int keeperUserId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(keeperUserId);

        KeeperUserId = keeperUserId;
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

    // A blank number clears the phone; callers remove the detached row.
    public void UpdatePhone(string countryCode, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            Phone = null;
            return;
        }

        var phone = Phone.Create(phoneNumber.Trim(), countryCode?.Trim() ?? "");
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

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    public override string ToString() => Name.Value;
}
