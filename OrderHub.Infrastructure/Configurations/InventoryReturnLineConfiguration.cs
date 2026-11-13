using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class InventoryReturnLineConfiguration : ModelConfigurationBase<InventoryReturnLine>
{
    public override void Configure(EntityTypeBuilder<InventoryReturnLine> builder)
    {
        base.Configure(builder);

        builder.ToTable("inventory_return_lines", table =>
        {
            table.HasCheckConstraint(
                "ck_inventory_return_lines_quantities",
                "intact_qty >= 0 AND damaged_qty >= 0 AND intact_qty + damaged_qty > 0 AND pending_settled_qty >= 0 AND pending_settled_qty <= intact_qty + damaged_qty");
        });

        builder.Ignore(line => line.TotalQty);

        builder.Property(line => line.ReturnId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.AllocationId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.OrderItemId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.ProductId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.IntactQty)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.DamagedQty)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.PendingSettledQty)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(line => line.Allocation)
            .WithMany()
            .HasForeignKey(line => line.AllocationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(line => line.OrderItem)
            .WithMany()
            .HasForeignKey(line => line.OrderItemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(line => line.Product)
            .WithMany()
            .HasForeignKey(line => line.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(line => new { line.ReturnId, line.AllocationId }).IsUnique();
        builder.HasIndex(line => line.AllocationId);
    }
}
