using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class InventoryCountConfiguration : ModelConfigurationBase<InventoryCount>
{
    public override void Configure(EntityTypeBuilder<InventoryCount> builder)
    {
        base.Configure(builder);

        builder.ToTable("inventory_counts", table =>
        {
            table.HasCheckConstraint(
                "ck_inventory_counts_decision",
                "status NOT IN ('Approved', 'Cancelled') OR (decided_at_utc IS NOT NULL AND decision_note IS NOT NULL)");
        });

        builder.Property(count => count.WarehouseId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(count => count.Scope)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(count => count.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(count => count.SnapshotAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(count => count.Version)
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(count => count.StartedByUserId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(count => count.CompletedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(count => count.DecidedByUserId)
            .HasColumnType("integer");

        builder.Property(count => count.DecidedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(count => count.DecisionNote)
            .HasMaxLength(InventoryCount.DecisionNoteMaxLength);

        builder.HasOne(count => count.Warehouse)
            .WithMany()
            .HasForeignKey(count => count.WarehouseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(count => count.StartedBy)
            .WithMany()
            .HasForeignKey(count => count.StartedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(count => count.DecidedBy)
            .WithMany()
            .HasForeignKey(count => count.DecidedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(count => count.Lines)
            .WithOne(line => line.Count)
            .HasForeignKey(line => line.CountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(count => new { count.WarehouseId, count.Status });
        builder.HasIndex(count => count.CreatedAt);
    }
}
