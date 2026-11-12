using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class OrderInventoryAllocationConfiguration : ModelConfigurationBase<OrderInventoryAllocation>
{
    public override void Configure(EntityTypeBuilder<OrderInventoryAllocation> builder)
    {
        base.Configure(builder);

        builder.ToTable("order_inventory_allocations", table =>
            table.HasCheckConstraint(
                "ck_order_inventory_allocations_quantity",
                "qty > 0 AND issued_qty >= 0 AND issued_qty <= qty"));

        builder.Property(allocation => allocation.OrderId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(allocation => allocation.OrderItemId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(allocation => allocation.ProductId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(allocation => allocation.SourceKind)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(allocation => allocation.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(allocation => allocation.WarehouseId)
            .HasColumnType("integer");

        builder.Property(allocation => allocation.SupplierId)
            .HasColumnType("integer");

        builder.Property(allocation => allocation.Qty)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(allocation => allocation.IssuedQty)
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Ignore(allocation => allocation.Remaining);

        builder.HasOne(allocation => allocation.Order)
            .WithMany(order => order.Allocations)
            .HasForeignKey(allocation => allocation.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(allocation => allocation.OrderItem)
            .WithMany()
            .HasForeignKey(allocation => allocation.OrderItemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(allocation => allocation.Warehouse)
            .WithMany()
            .HasForeignKey(allocation => allocation.WarehouseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(allocation => allocation.Supplier)
            .WithMany()
            .HasForeignKey(allocation => allocation.SupplierId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(allocation => new
            {
                allocation.OrderItemId,
                allocation.SourceKind,
                allocation.WarehouseId
            })
            .IsUnique();

        builder.HasIndex(allocation => new { allocation.OrderId, allocation.Status });
    }
}
