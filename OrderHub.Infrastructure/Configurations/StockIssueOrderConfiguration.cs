using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class StockIssueOrderConfiguration : ModelConfigurationBase<StockIssueOrder>
{
    public override void Configure(EntityTypeBuilder<StockIssueOrder> builder)
    {
        base.Configure(builder);

        builder.ToTable("stock_issue_orders", table =>
        {
            table.HasCheckConstraint(
                "ck_stock_issue_orders_numbering",
                "numbering_party_id > 0 AND work_record_number > 0 AND sequence_year > 0 AND sequence_month BETWEEN 1 AND 12");
            table.HasCheckConstraint(
                "ck_stock_issue_orders_cancel_reason",
                "status <> 'CancelRequested' OR (cancel_reason IS NOT NULL AND length(trim(cancel_reason)) > 0)");
        });

        builder.Property(order => order.OrderId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(order => order.WarehouseId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(order => order.RequestedByUserId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(order => order.AssignedUserId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(order => order.CancelRequestedByUserId)
            .HasColumnType("integer");

        builder.Property(order => order.CancelReason)
            .HasMaxLength(StockIssueOrder.ReasonMaxLength);

        builder.Property(order => order.CancelRequestedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(order => order.Version)
            .IsConcurrencyToken()
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(order => order.NumberingPartyId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(order => order.WorkRecordNumber)
            .IsRequired();

        builder.Property(order => order.SequenceYear)
            .IsRequired();

        builder.Property(order => order.SequenceMonth)
            .IsRequired();

        builder.Ignore(order => order.StatusHistory);
        builder.Ignore("_statusHistory");

        builder.HasOne(order => order.Order)
            .WithMany()
            .HasForeignKey(order => order.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(order => order.Warehouse)
            .WithMany()
            .HasForeignKey(order => order.WarehouseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(order => order.RequestedBy)
            .WithMany()
            .HasForeignKey(order => order.RequestedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(order => order.AssignedTo)
            .WithMany()
            .HasForeignKey(order => order.AssignedUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(order => order.CancelRequestedBy)
            .WithMany()
            .HasForeignKey(order => order.CancelRequestedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(order => order.Lines)
            .WithOne(line => line.IssueOrder)
            .HasForeignKey(line => line.IssueOrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(order => new { order.WarehouseId, order.Status });
        builder.HasIndex(order => order.OrderId);
        builder.HasIndex(order => new
            {
                order.NumberingPartyId,
                order.SequenceYear,
                order.SequenceMonth,
                order.WorkRecordNumber
            })
            .IsUnique();
    }
}
