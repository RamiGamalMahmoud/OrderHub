using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class StockReceiptLineConfiguration : ModelConfigurationBase<StockReceiptLine>
{
    public override void Configure(EntityTypeBuilder<StockReceiptLine> builder)
    {
        base.Configure(builder);

        builder.ToTable("stock_receipt_lines", table =>
            table.HasCheckConstraint("ck_stock_receipt_lines_quantity", "quantity > 0"));

        builder.Property(line => line.StockReceiptId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.ProductId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.Quantity)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(line => line.Product)
            .WithMany()
            .HasForeignKey(line => line.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(line => new { line.StockReceiptId, line.ProductId }).IsUnique();
    }
}
