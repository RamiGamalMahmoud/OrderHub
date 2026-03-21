using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OrderDeliveryStepConfiguration : ModelConfigurationBase<OrderDeliveryStep>
{
    public override void Configure(EntityTypeBuilder<OrderDeliveryStep> builder)
    {
        base.Configure(builder);

        builder.ToTable("order_delivery_steps");

        builder.Property(step => step.StepOrder)
            .IsRequired();

        builder.Property(step => step.DeliveryMethod)
            .HasConversion<string>()
            .IsRequired();

        builder.HasOne(step => step.Order)
            .WithMany(order => order.DeliverySteps)
            .HasForeignKey(step => step.OrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(step => step.Deliveryman)
            .WithMany()
            .HasForeignKey(step => step.DeliverymanId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(step => step.ShippingCarrier)
            .WithMany()
            .HasForeignKey(step => step.ShippingCarrierId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasIndex(step => new { step.OrderId, step.StepOrder })
            .IsUnique();
    }
}
