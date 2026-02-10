using OrderHub.Domain.ValueObjects;
using System;

namespace OrderHub.Domain.Models
{
    public class ShippingCarrier : ModelBase
    {
        private ShippingCarrier() { }
        public ShippingCarrier(string name, decimal shippingCost, Phone phone, Address address)
        {
            Rename(name);
            ChangeShippingCost(shippingCost);
            ChangeAddress(address);
            UpdatePhone(phone);
        }

        public EntityName Name { get; private set; }
        public Phone Phone { get; private set; }
        public Address Address { get; private set; }
        public Money ShippingCost { get; private set; }

        public void ChangeShippingCost(decimal shippingCost)
        {
            if (shippingCost < 0)
                throw new ArgumentOutOfRangeException("Shipping coast can not be negative.");
            ShippingCost = new Money(shippingCost);
        }

        public void Rename(string newName)
        {
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentNullException("Shipping carrier name can not be empty.");

            Name = new EntityName(newName);
        }

        public void ChangeAddress(City city, string street)
        {
            ArgumentNullException.ThrowIfNull(city);
            ArgumentNullException.ThrowIfNull(street);
            if (Address is null)
                throw new System.InvalidOperationException("Address not set.");
            Address.ChangeCity(city);
            Address.ChangeStreet(street);
        }

        public void ChangeAddress(Address address)
        {
            ArgumentNullException.ThrowIfNull(address);
            Address = address;
        }

        public void UpdatePhone(Phone phone)
        {
            if (phone is not null)
            {
                Phone = phone;
            }
        }
    }
}
