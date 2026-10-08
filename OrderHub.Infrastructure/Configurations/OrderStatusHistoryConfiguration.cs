using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_history", table => table.HasCheckConstraint("ck_order_history_attribution",
            "source = 'LegacyMigration' OR (changed_by_user_id IS NOT NULL AND changed_by_user_name IS NOT NULL AND " +
            "((new_status NOT IN ('Cancelled','DeliveryFailed') AND source <> 'DirectDelivery') OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)))"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ChangedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(200);
        builder.Property(x => x.ChangedByUserName).HasMaxLength(150);
        builder.Property(x => x.OriginalStatus).HasMaxLength(30);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.OrderId, x.OrderVersion, x.Id });
    }
}
