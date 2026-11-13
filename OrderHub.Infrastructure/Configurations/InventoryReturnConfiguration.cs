using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class InventoryReturnConfiguration : ModelConfigurationBase<InventoryReturn>
{
    public override void Configure(EntityTypeBuilder<InventoryReturn> builder)
    {
        base.Configure(builder);

        builder.ToTable("inventory_returns");

        builder.Property(record => record.WarehouseId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(record => record.OrderId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(record => record.StockIssueOrderId)
            .HasColumnType("integer");

        builder.Property(record => record.RecordedByUserId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(record => record.Note)
            .HasMaxLength(InventoryReturn.NoteMaxLength);

        builder.Property(record => record.EffectiveAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(record => record.Warehouse)
            .WithMany()
            .HasForeignKey(record => record.WarehouseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.Order)
            .WithMany()
            .HasForeignKey(record => record.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.StockIssueOrder)
            .WithMany()
            .HasForeignKey(record => record.StockIssueOrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(record => record.RecordedBy)
            .WithMany()
            .HasForeignKey(record => record.RecordedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(record => record.Lines)
            .WithOne(line => line.Return)
            .HasForeignKey(line => line.ReturnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(record => record.WarehouseId);
        builder.HasIndex(record => record.OrderId);
    }
}
