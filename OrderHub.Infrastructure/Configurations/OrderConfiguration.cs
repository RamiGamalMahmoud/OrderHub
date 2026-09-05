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

        builder.Property(x => x.ClientId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.Client)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.ClientId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.OrderNumber)
            .HasColumnName("order_number")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Ignore(x => x.Total);

        builder.Property(x => x.DeliveryMethod)
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasConversion<string>()
            .IsRequired(false);

        builder.Property(x => x.OrderStatus)
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasConversion<string>()
            .HasDefaultValue(OrderStatus.Pending)
            .IsRequired();

        builder.Property(x => x.ShippingCarrierId)
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.ShippingCarrier)
            .WithMany()
            .HasForeignKey(x => x.ShippingCarrierId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.DeliverymanId)
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.Deliveryman)
            .WithMany()
            .HasForeignKey(x => x.DeliverymanId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.PaymentMethodId)
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.PaymentMethod)
            .WithMany()
            .HasForeignKey(x => x.PaymentMethodId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.OrderItems)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DeliverySteps)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.EntitySequences)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}