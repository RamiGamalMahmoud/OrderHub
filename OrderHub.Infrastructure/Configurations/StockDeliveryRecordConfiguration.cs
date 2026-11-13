using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class StockDeliveryRecordConfiguration : ModelConfigurationBase<StockDeliveryRecord>
{
    public override void Configure(EntityTypeBuilder<StockDeliveryRecord> builder)
    {
        base.Configure(builder);

        builder.ToTable("stock_delivery_records", table =>
            table.HasCheckConstraint("ck_stock_delivery_records_quantity", "quantity > 0"));

        builder.Property(record => record.OrderId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(record => record.OrderItemId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(record => record.AllocationId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(record => record.ProductId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(record => record.Quantity)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(record => record.TripId)
            .HasColumnType("integer");

        builder.Property(record => record.ShipmentId)
            .HasColumnType("integer");

        builder.Property(record => record.Note)
            .HasMaxLength(200);

        builder.Property(record => record.RecordedByUserId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(record => record.EffectiveAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(record => record.Order)
            .WithMany()
            .HasForeignKey(record => record.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.OrderItem)
            .WithMany()
            .HasForeignKey(record => record.OrderItemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.Allocation)
            .WithMany()
            .HasForeignKey(record => record.AllocationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.Product)
            .WithMany()
            .HasForeignKey(record => record.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.Trip)
            .WithMany()
            .HasForeignKey(record => record.TripId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.Shipment)
            .WithMany()
            .HasForeignKey(record => record.ShipmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.RecordedBy)
            .WithMany()
            .HasForeignKey(record => record.RecordedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(record => record.OrderId);
        builder.HasIndex(record => record.AllocationId);
        builder.HasIndex(record => record.OrderItemId);
    }
}
