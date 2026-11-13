using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class StockIssueBatchConfiguration : ModelConfigurationBase<StockIssueBatch>
{
    public override void Configure(EntityTypeBuilder<StockIssueBatch> builder)
    {
        base.Configure(builder);

        builder.ToTable("stock_issue_batches", table =>
            table.HasCheckConstraint("ck_stock_issue_batches_quantity", "quantity > 0"));

        builder.Property(batch => batch.IssueOrderLineId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(batch => batch.Quantity)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(batch => batch.RecordedByUserId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(batch => batch.EffectiveAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(batch => batch.ShortfallReason)
            .HasMaxLength(StockIssueBatch.ReasonMaxLength);

        builder.HasOne(batch => batch.RecordedBy)
            .WithMany()
            .HasForeignKey(batch => batch.RecordedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(batch => new { batch.IssueOrderLineId, batch.CreatedAt });
    }
}
