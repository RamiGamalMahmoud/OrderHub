using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class StockIssueLineConfiguration : ModelConfigurationBase<StockIssueLine>
{
    public override void Configure(EntityTypeBuilder<StockIssueLine> builder)
    {
        base.Configure(builder);

        builder.ToTable("stock_issue_lines", table =>
            table.HasCheckConstraint("ck_stock_issue_lines_quantity", "requested_qty > 0"));

        builder.Property(line => line.IssueOrderId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.AllocationId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.ProductId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(line => line.RequestedQty)
            .HasColumnType("integer")
            .IsRequired();

        builder.Ignore(line => line.IssuedQty);
        builder.Ignore(line => line.RemainingQty);

        builder.HasOne(line => line.Allocation)
            .WithMany()
            .HasForeignKey(line => line.AllocationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(line => line.Product)
            .WithMany()
            .HasForeignKey(line => line.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(line => line.Batches)
            .WithOne(batch => batch.IssueOrderLine)
            .HasForeignKey(batch => batch.IssueOrderLineId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(line => new { line.IssueOrderId, line.AllocationId }).IsUnique();
    }
}
