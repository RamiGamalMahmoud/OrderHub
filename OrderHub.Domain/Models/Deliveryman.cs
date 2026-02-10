using OrderHub.Domain.ValueObjects;

namespace OrderHub.Domain.Models
{
    public class Deliveryman : ModelBase
    {
        private Deliveryman() { }

        public Deliveryman(string name, City city)
        {
            Rename(name);
            ChangeCity(city);
        }

        public EntityName Name { get; private set; }
        public City City { get; private set; }
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
    }
}
