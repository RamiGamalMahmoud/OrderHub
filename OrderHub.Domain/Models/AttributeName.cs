namespace OrderHub.Domain.Models
{
    public class AttributeName : ModelBase
    {
        public string Name { get; private set; } 
        private AttributeName() { }
        public AttributeName(string name) => Name = name;
        public void Rename(string name) => Name = name;
    }
}
