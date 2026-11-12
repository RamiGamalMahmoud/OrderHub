using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements", table =>
        {
            table.HasCheckConstraint("ck_inventory_movements_quantity", "quantity > 0");
            table.HasCheckConstraint("ck_inventory_movements_reversal_reference",
                "(movement_type = 'Reversal' AND reversal_of_movement_id IS NOT NULL) OR " +
                "(movement_type <> 'Reversal' AND reversal_of_movement_id IS NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType("integer")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.WarehouseId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.Direction)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.MovementType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.EffectiveAtUtc)
            .HasColumnName("effective_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedByUserId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(500);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Warehouse>()
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.WarehouseId, x.ProductId, x.EffectiveAtUtc });
        builder.HasIndex(x => new { x.ProductId, x.EffectiveAtUtc });

        builder.HasIndex(x => x.ReversalOfMovementId)
            .IsUnique()
            .HasFilter("\"reversal_of_movement_id\" IS NOT NULL");
    }
}
