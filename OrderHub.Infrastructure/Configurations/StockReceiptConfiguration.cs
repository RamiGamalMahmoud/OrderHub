using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class StockReceiptConfiguration : ModelConfigurationBase<StockReceipt>
{
    public override void Configure(EntityTypeBuilder<StockReceipt> builder)
    {
        base.Configure(builder);

        builder.ToTable("stock_receipts");

        builder.Property(receipt => receipt.WarehouseId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(receipt => receipt.ReceiptType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(receipt => receipt.SupplierId)
            .HasColumnType("integer");

        builder.Property(receipt => receipt.ReferenceNote)
            .HasMaxLength(StockReceipt.ReferenceNoteMaxLength);

        builder.Property(receipt => receipt.CreatedByUserId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(receipt => receipt.Warehouse)
            .WithMany()
            .HasForeignKey(receipt => receipt.WarehouseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(receipt => receipt.Supplier)
            .WithMany()
            .HasForeignKey(receipt => receipt.SupplierId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(receipt => receipt.CreatedBy)
            .WithMany()
            .HasForeignKey(receipt => receipt.CreatedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(receipt => receipt.Lines)
            .WithOne(line => line.StockReceipt)
            .HasForeignKey(line => line.StockReceiptId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(receipt => receipt.Attachments)
            .WithOne(attachment => attachment.StockReceipt)
            .HasForeignKey(attachment => attachment.StockReceiptId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(receipt => new { receipt.WarehouseId, receipt.CreatedAt });
    }
}
