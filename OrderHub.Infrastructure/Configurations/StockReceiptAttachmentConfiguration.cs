using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class StockReceiptAttachmentConfiguration : ModelConfigurationBase<StockReceiptAttachment>
{
    public override void Configure(EntityTypeBuilder<StockReceiptAttachment> builder)
    {
        base.Configure(builder);

        builder.ToTable("stock_receipt_attachments", table =>
            table.HasCheckConstraint("ck_stock_receipt_attachments_size", "size_bytes > 0"));

        builder.Property(attachment => attachment.StockReceiptId)
            .HasColumnType("integer");

        builder.Property(attachment => attachment.WarehouseId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(attachment => attachment.StoredFileName)
            .HasMaxLength(StockReceiptAttachment.StoredFileNameMaxLength)
            .IsRequired();

        builder.Property(attachment => attachment.OriginalFileName)
            .HasMaxLength(StockReceiptAttachment.OriginalFileNameMaxLength)
            .IsRequired();

        builder.Property(attachment => attachment.Description)
            .HasMaxLength(StockReceiptAttachment.DescriptionMaxLength);

        builder.Property(attachment => attachment.SizeBytes)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(attachment => attachment.ContentType)
            .HasMaxLength(StockReceiptAttachment.ContentTypeMaxLength)
            .IsRequired();

        builder.Property(attachment => attachment.UploadedByUserId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(attachment => attachment.Warehouse)
            .WithMany()
            .HasForeignKey(attachment => attachment.WarehouseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(attachment => attachment.UploadedBy)
            .WithMany()
            .HasForeignKey(attachment => attachment.UploadedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(attachment => attachment.StoredFileName).IsUnique();

        builder.HasIndex(attachment => new
        {
            attachment.WarehouseId,
            attachment.UploadedByUserId,
            attachment.StockReceiptId
        });
    }
}
