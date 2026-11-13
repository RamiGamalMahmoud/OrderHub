using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class InventoryRebuildLogConfiguration : ModelConfigurationBase<InventoryRebuildLog>
{
    public override void Configure(EntityTypeBuilder<InventoryRebuildLog> builder)
    {
        base.Configure(builder);

        builder.ToTable("inventory_rebuild_logs", table =>
        {
            table.HasCheckConstraint(
                "ck_inventory_rebuild_logs_scope_warehouse",
                "(scope = 'SingleWarehouse' AND warehouse_id IS NOT NULL) OR (scope = 'AllWarehouses' AND warehouse_id IS NULL)");
            table.HasCheckConstraint(
                "ck_inventory_rebuild_logs_pairs",
                "inspected_pairs >= 0 AND changed_pairs >= 0 AND changed_pairs <= inspected_pairs");
        });

        builder.Property(log => log.PerformedByUserId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(log => log.Scope)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(log => log.WarehouseId)
            .HasColumnType("integer");

        builder.Property(log => log.InspectedPairs)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(log => log.ChangedPairs)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(log => log.PerformedBy)
            .WithMany()
            .HasForeignKey(log => log.PerformedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(log => log.Warehouse)
            .WithMany()
            .HasForeignKey(log => log.WarehouseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(log => log.Lines)
            .WithOne(line => line.RebuildLog)
            .HasForeignKey(line => line.RebuildLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(log => log.CreatedAt);
    }
}
