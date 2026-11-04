using OrderHub.Domain.ValueObjects;
using System;

namespace OrderHub.Domain.Models;

public class Supplier : ModelBase
{
    private Supplier()
    {
    }

    public Supplier(
        string name,
        TimeOnly? openAt,
        TimeOnly? closeAt,
        Address address,
        Phone phone)
    {
        UpdateName(name);
        UpdateAddress(address);
        UpdatePhone(phone);
        UpdateBusinessHours(openAt, closeAt);
    }

    public Supplier(string name)
    {
        UpdateName(name);
    }

    public EntityName Name { get; private set; }

    public Phone Phone { get; private set; }

    public Address Address { get; private set; }

    // ساعات العمل اختيارية (قد تكون فارغة) مثل الخاصيتين Address وPhone.
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
        Name = new EntityName(name?.Trim());
    }

    public void UpdateAddress(Address address)
    {
        if (address is not null)
        {
            Address = address;
        }
    }

    // ساعات العمل اختيارية بالكامل: الاثنان معًا
    // أو أي من أحدهما أو لا شيء على الإطلاق.
    public void UpdateBusinessHours(TimeOnly? openAt, TimeOnly? closeAt)
    {
        BusinessHours = openAt is null && closeAt is null
            ? null
            : new BusinessHours(openAt, closeAt);
    }
}
