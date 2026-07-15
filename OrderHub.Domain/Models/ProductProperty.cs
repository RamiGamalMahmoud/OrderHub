namespace OrderHub.Domain.Models;

public class ProductProperty : ModelBase
{
    public int ProductId { get; set; }

    public int PropertyId { get; set; }

    public bool IsRequired { get; set; }

    public Product Product { get; set; } = null!;

    public Property Property { get; set; } = null!;
}
