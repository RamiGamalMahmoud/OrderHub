using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class InventoryApprovalRequestConfiguration : ModelConfigurationBase<InventoryApprovalRequest>
{
    public override void Configure(EntityTypeBuilder<InventoryApprovalRequest> builder)
    {
        base.Configure(builder);

        builder.ToTable("inventory_approval_requests", table =>
        {
            table.HasCheckConstraint(
                "ck_inventory_approval_requests_single_target",
                "(request_type = 'AllocationEdit' AND order_id IS NOT NULL AND stock_issue_order_id IS NULL AND inventory_movement_id IS NULL AND warehouse_id IS NULL AND product_id IS NULL AND disposal_quantity IS NULL) OR " +
                "(request_type = 'IssueOrderCancel' AND order_id IS NULL AND stock_issue_order_id IS NOT NULL AND inventory_movement_id IS NULL AND warehouse_id IS NULL AND product_id IS NULL AND disposal_quantity IS NULL) OR " +
                "(request_type = 'MovementReversal' AND order_id IS NULL AND stock_issue_order_id IS NULL AND inventory_movement_id IS NOT NULL AND warehouse_id IS NULL AND product_id IS NULL AND disposal_quantity IS NULL) OR " +
                "(request_type = 'DamagedDisposal' AND order_id IS NULL AND stock_issue_order_id IS NULL AND inventory_movement_id IS NULL AND warehouse_id IS NOT NULL AND product_id IS NOT NULL AND disposal_quantity > 0)");
            table.HasCheckConstraint(
                "ck_inventory_approval_requests_reason",
                "length(trim(reason)) > 0");
            table.HasCheckConstraint(
                "ck_inventory_approval_requests_decision",
                "status NOT IN ('Approved', 'Rejected', 'Stale') OR (decided_at_utc IS NOT NULL AND decision_note IS NOT NULL)");
        });

        builder.Property(request => request.RequestType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(request => request.OrderId)
            .HasColumnType("integer");

        builder.Property(request => request.StockIssueOrderId)
            .HasColumnType("integer");

        builder.Property(request => request.InventoryMovementId)
            .HasColumnType("integer");

        builder.Property(request => request.WarehouseId)
            .HasColumnType("integer");

        builder.Property(request => request.ProductId)
            .HasColumnType("integer");

        builder.Property(request => request.DisposalQuantity)
            .HasColumnType("integer");

        builder.Property(request => request.Payload)
            .HasColumnType("jsonb");

        builder.Property(request => request.Reason)
            .HasMaxLength(InventoryApprovalRequest.ReasonMaxLength)
            .IsRequired();

        builder.Property(request => request.TargetVersion)
            .HasColumnType("integer");

        builder.Property(request => request.Version)
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(request => request.RequestedByUserId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(request => request.DecidedByUserId)
            .HasColumnType("integer");

        builder.Property(request => request.DecidedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(request => request.DecisionNote)
            .HasMaxLength(InventoryApprovalRequest.DecisionNoteMaxLength);

        builder.HasOne(request => request.Order)
            .WithMany()
            .HasForeignKey(request => request.OrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.StockIssueOrder)
            .WithMany()
            .HasForeignKey(request => request.StockIssueOrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.InventoryMovement)
            .WithMany()
            .HasForeignKey(request => request.InventoryMovementId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.Warehouse)
            .WithMany()
            .HasForeignKey(request => request.WarehouseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.Product)
            .WithMany()
            .HasForeignKey(request => request.ProductId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.RequestedBy)
            .WithMany()
            .HasForeignKey(request => request.RequestedByUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(request => request.DecidedBy)
            .WithMany()
            .HasForeignKey(request => request.DecidedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(request => new { request.Status, request.RequestType });
        builder.HasIndex(request => request.OrderId)
            .IsUnique()
            .HasFilter("\"status\" = 'Pending' AND \"request_type\" = 'AllocationEdit'");
        builder.HasIndex(request => request.StockIssueOrderId)
            .IsUnique()
            .HasFilter("\"status\" = 'Pending' AND \"request_type\" = 'IssueOrderCancel'");
        builder.HasIndex(request => request.InventoryMovementId)
            .IsUnique()
            .HasFilter("\"status\" = 'Pending' AND \"request_type\" = 'MovementReversal'");
        builder.HasIndex(request => new { request.WarehouseId, request.ProductId })
            .IsUnique()
            .HasFilter("\"status\" = 'Pending' AND \"request_type\" = 'DamagedDisposal'");
    }
}
