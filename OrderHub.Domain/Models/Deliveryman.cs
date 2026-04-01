using OrderHub.Domain.ValueObjects;

namespace OrderHub.Domain.Models
{
    public class Deliveryman : ModelBase
    {
        private Deliveryman() { }

        public Deliveryman(string name, City city, string phoneNumber, WhatsappGroup whatsappGroup = null)
        {
            Rename(name);
            ChangeCity(city);
            PhoneNumber = phoneNumber;
            ChangeWhatsappGroup(whatsappGroup);
        }

        public string PhoneNumber { get; set; }
        public EntityName Name { get; private set; }
        public City City { get; private set; }
        public WhatsappGroup WhatsappGroup { get; private set; }

        public void Rename(string name)
        {
            if (Name is not null && Name.Value == name)
                return;
            Name = new EntityName(name);
        }
        public void ChangeCity(City city)
        {
            if (City == city)
                return;
            City = city;
        }

        public void ChangeWhatsappGroup(WhatsappGroup whatsappGroup)
        {
            if (WhatsappGroup == whatsappGroup)
                return;

            WhatsappGroup = whatsappGroup;
        }
    }
}
