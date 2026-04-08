using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OrderItemAttributeConfiguration : ModelConfigurationBase<OrderItemAttribute>
{
    public override void Configure(EntityTypeBuilder<OrderItemAttribute> builder)
    {
        base.Configure(builder);

        builder.ToTable("order_item_attributes");

        builder.Property(attribute => attribute.Name)
            .HasColumnType("VARCHAR(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(attribute => attribute.Value)
            .HasColumnType("VARCHAR(500)")
            .HasMaxLength(500)
            .IsRequired();
    }
}
