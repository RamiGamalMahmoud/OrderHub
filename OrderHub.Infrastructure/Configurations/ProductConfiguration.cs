using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class ProductConfiguration : ModelConfigurationBase<Product>
{
    public override void Configure(EntityTypeBuilder<Product> builder)
    {
        base.Configure(builder);

        builder.OwnsOne(product => product.Name,
            navigationBuilder =>
            {
                navigationBuilder.Property(entityName => entityName.Value)
                .HasColumnName("name")
                .HasColumnType("VARCHAR(100)")
                .HasMaxLength(100)
                .IsRequired();
            });

        builder.Navigation(product => product.Name).IsRequired();

        builder.OwnsOne(product => product.Price, navigationBuilder => navigationBuilder.Property(price => price.Value)
            .HasColumnName("price")
            .HasColumnType("DECIMAL(18,2)")
            .IsRequired());

        builder.Navigation(product => product.Price).IsRequired();

        builder.Property(product => product.Code)
            .HasColumnName("code")
            .HasColumnType("VARCHAR(10)")
            .HasMaxLength(10)
            .IsRequired();

        builder.HasOne(product => product.Category)
            .WithMany(c => c.Products)
            .HasForeignKey("category_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(product => product.Suppliers).WithMany();
    }
}
