using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class ProductPropertyConfiguration : ModelConfigurationBase<ProductProperty>
{
    public override void Configure(EntityTypeBuilder<ProductProperty> builder)
    {
        base.Configure(builder);

        builder.ToTable("product_properties");

        builder.Property(x => x.ProductId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.PropertyId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.IsRequired)
            .HasColumnType("boolean")
            .IsRequired();

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Properties)
            .HasForeignKey(x => x.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Property)
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.ProductId,
            x.PropertyId
        });
    }
}