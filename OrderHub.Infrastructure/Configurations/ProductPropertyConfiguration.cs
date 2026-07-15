using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class ProductPropertyConfiguration : ModelConfigurationBase<ProductProperty>
{
    public override void Configure(EntityTypeBuilder<ProductProperty> builder)
    {
        base.Configure(builder);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(x => x.Property)
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.Property(x => x.IsRequired);

        builder.HasIndex(x => new { x.ProductId, x.PropertyId });
    }
}
