using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OrderConfiguration : ModelConfigurationBase<Order>
{
    public override void Configure(EntityTypeBuilder<Order> builder)
    {
        base.Configure(builder);

        builder.ToTable("orders");

        builder.HasOne(o => o.Client)
            .WithMany(c => c.Orders)
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

        builder.HasMany(o => o.DeliverySteps)
            .WithOne(step => step.Order)
            .HasForeignKey(step => step.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.EntitySequences)
            .WithOne(sequence => sequence.Order)
            .HasForeignKey(sequence => sequence.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(o => o.DeliveryMethod)
            .HasConversion<string>()
            .IsRequired(false);

        builder.Property(o => o.OrderStatus)
            .HasConversion<string>()
            .HasDefaultValue(OrderStatus.Pending)
            .IsRequired();

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

        builder.HasOne(o => o.PaymentMethod)
            .WithMany()
            .HasForeignKey(o => o.PaymentMethodId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
