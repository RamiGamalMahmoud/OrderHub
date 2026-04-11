using OrderHub.Domain.ValueObjects;
using System.Collections.Generic;

namespace OrderHub.Domain.Models
{
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
        public string Location { get; set; }
        public void UpdateName(string name)
        {
            Name = new EntityName(name);
        }

        private List<Order> _orders = new List<Order>();
        public IReadOnlyCollection<Order> Orders => _orders.AsReadOnly();

        public Phone Phone { get; private set; }

        public Address Address { get; private set; }

        public void UpdatePhone(string countryCode, string number)
        {
            if(Phone is null)
            {
                Phone = Phone.Create(number, countryCode).Value;
            }

            Phone.ChangeNumber(number, countryCode);
        }

        public void UpdateAddress(City city, string street)
        {
            if(Address is null)
            {
                Address = Address.Create(street, city).Value;
            }

            Address.ChangeCity(city);
            Address.ChangeStreet(street);
        }
    }
}
