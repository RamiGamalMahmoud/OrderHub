using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class OrderDeliveryMovementConfiguration : IEntityTypeConfiguration<OrderDeliveryMovement>
{
    public void Configure(EntityTypeBuilder<OrderDeliveryMovement> builder)
    {
        builder.ToTable("order_delivery_movements", table => table.HasCheckConstraint("ck_movement_attribution",
            "changed_by_user_id IS NOT NULL AND changed_by_user_name IS NOT NULL AND " +
            "length(trim(reason)) BETWEEN 1 AND 200"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PreviousMethod).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.NextMethod).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.PreviousPartyName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NextPartyName).HasMaxLength(100);
        builder.Property(x => x.Reason).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ChangedByUserName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ChangedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasOne(x => x.DeliveryStep).WithMany().HasForeignKey(x => x.DeliveryStepId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReplacementStep).WithMany().HasForeignKey(x => x.ReplacementStepId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OldShipment).WithMany().HasForeignKey(x => x.OldShipmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OldDeliveryTrip).WithMany().HasForeignKey(x => x.OldDeliveryTripId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.NewShipment).WithMany().HasForeignKey(x => x.NewShipmentId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.NewDeliveryTrip).WithMany().HasForeignKey(x => x.NewDeliveryTripId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.OrderId, x.OrderVersion, x.Id });
        builder.HasIndex(x => new { x.OrderId, x.OrderVersion, x.ChangedAtUtc });
    }
}
