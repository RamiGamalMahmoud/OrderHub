using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OrderItemPropertyConfiguration
    : ModelConfigurationBase<OrderItemProperty>
{
    public override void Configure(EntityTypeBuilder<OrderItemProperty> builder)
    {
        base.Configure(builder);

        builder.ToTable("order_item_properties");
        builder.Property(x => x.PropertyId).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Property(x => x.Value).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Property).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(x => x.OrderItemId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.PropertyId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.Value)
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.HasOne(x => x.OrderItem)
            .WithMany(x => x.Properties)
            .HasForeignKey(x => x.OrderItemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Property)
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.OrderItemId,
            x.PropertyId
        })
        .IsUnique();
    }
}
