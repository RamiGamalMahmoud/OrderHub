using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class DeliveryStepStatusHistoryConfiguration : IEntityTypeConfiguration<DeliveryStepStatusHistory>
{
    public void Configure(EntityTypeBuilder<DeliveryStepStatusHistory> builder)
    {
        builder.ToTable("delivery_step_status_history", table => table.HasCheckConstraint("ck_step_history_attribution",
            "source = 'LegacyMigration' OR (changed_by_user_id IS NOT NULL AND changed_by_user_name IS NOT NULL AND " +
            "(new_status NOT IN ('Failed','Cancelled') OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)))"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PreviousStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ChangedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(200);
        builder.Property(x => x.ChangedByUserName).HasMaxLength(150);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DeliveryStep).WithMany().HasForeignKey(x => x.DeliveryStepId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.DeliveryStepId, x.OrderVersion, x.Id });
        builder.HasIndex(x => new { x.OrderId, x.OrderVersion, x.Id });
    }
}
