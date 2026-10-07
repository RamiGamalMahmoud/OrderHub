using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OrderDeliveryStepConfiguration
    : ModelConfigurationBase<OrderDeliveryStep>
{
    public override void Configure(EntityTypeBuilder<OrderDeliveryStep> builder)
    {
        base.Configure(builder);

        builder.ToTable("order_delivery_steps");

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).HasDefaultValue(OrderHub.Domain.Enums.DeliveryStatus.Pending);

        builder.Property(x => x.StepOrder)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.DeliveryMethod)
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.OrderId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.Order)
            .WithMany(x => x.DeliverySteps)
            .HasForeignKey(x => x.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.DeliverymanId)
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.Deliveryman)
            .WithMany()
            .HasForeignKey(x => x.DeliverymanId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ShippingCarrierId)
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.ShippingCarrier)
            .WithMany()
            .HasForeignKey(x => x.ShippingCarrierId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
        {
            x.OrderId,
            x.StepOrder
        })
        .IsUnique();
    }
}
