using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class InventoryCountLineConfiguration : ModelConfigurationBase<InventoryCountLine>
{
    public override void Configure(EntityTypeBuilder<InventoryCountLine> builder)
    {
        base.Configure(builder);

        builder.ToTable("inventory_count_lines", table =>
        {
            table.HasCheckConstraint(
                "ck_inventory_count_lines_counted",
                "(counted_qty IS NULL OR counted_qty >= 0) AND (counted_damaged_qty IS NULL OR counted_damaged_qty >= 0)");
        });

        builder.Ignore(line => line.Delta);
        builder.Ignore(line => line.DamagedDelta);
        builder.Ignore(line => line.IsCounted);

        builder.Property(line => line.CountId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.WarehouseId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.ProductId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.BookQty)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.BookDamagedQty)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.CountedQty)
            .HasColumnType("integer");

        builder.Property(line => line.CountedDamagedQty)
            .HasColumnType("integer");

        builder.Property(line => line.SnapshotAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(line => line.SnapshotRecordedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(line => line.ReviewedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(line => line.ReviewDecision)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(line => line.ReviewNote)
            .HasMaxLength(InventoryCountLine.ReviewNoteMaxLength);

        builder.Property(line => line.IsActive)
            .HasColumnType("boolean")
            .IsRequired();

        builder.HasOne(line => line.Warehouse)
            .WithMany()
            .HasForeignKey(line => line.WarehouseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(line => line.Product)
            .WithMany()
            .HasForeignKey(line => line.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(line => line.CountId);
        builder.HasIndex(line => new { line.WarehouseId, line.ProductId })
            .IsUnique()
            .HasFilter("\"is_active\" = TRUE");
    }
}
