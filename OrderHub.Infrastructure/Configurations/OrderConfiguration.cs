using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OrderConfiguration : ModelConfigurationBase<Order>
{
    public override void Configure(EntityTypeBuilder<Order> builder)
    {
        base.Configure(builder);

        builder.ToTable("orders");

        builder.HasOne(o => o.Client)
            .WithMany()
            .HasForeignKey(o => o.ClientId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.OrderNumber)
            .HasColumnType("VARCHAR(20)")
            .IsRequired();

        builder.Ignore(o => o.Total);

        builder.HasMany(o => o.OrderItems)
            .WithOne()
            .HasForeignKey(o => o.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.OrderStatus)
            .WithMany()
            .HasForeignKey(o => o.OrderStatusId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.DeliveryMethod)
            .HasConversion<string>()
            .IsRequired(false);

        builder.HasOne(o => o.ShippingCarrier)
            .WithMany()
            .HasForeignKey(o=> o.ShippingCarrierId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(o => o.Deliveryman)
            .WithMany()
            .HasForeignKey(o => o.DeliverymanId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
