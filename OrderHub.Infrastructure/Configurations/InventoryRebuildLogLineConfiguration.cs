using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class InventoryRebuildLogLineConfiguration : ModelConfigurationBase<InventoryRebuildLogLine>
{
    public override void Configure(EntityTypeBuilder<InventoryRebuildLogLine> builder)
    {
        base.Configure(builder);

        builder.ToTable("inventory_rebuild_log_lines", table =>
        {
            table.HasCheckConstraint(
                "ck_inventory_rebuild_log_lines_quantities",
                "before_stock_on_hand >= 0 AND before_reserved_on_hand >= 0 AND before_damaged_qty >= 0 " +
                "AND after_stock_on_hand >= 0 AND after_reserved_on_hand >= 0 AND after_damaged_qty >= 0");
        });

        builder.Property(line => line.RebuildLogId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.WarehouseId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.ProductId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.BeforeStockOnHand)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.BeforeReservedOnHand)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.BeforeDamagedQty)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.AfterStockOnHand)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.AfterReservedOnHand)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.AfterDamagedQty)
            .HasColumnType("integer")
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

        builder.HasIndex(line => line.RebuildLogId);
        builder.HasIndex(line => new { line.WarehouseId, line.ProductId });
    }
}
