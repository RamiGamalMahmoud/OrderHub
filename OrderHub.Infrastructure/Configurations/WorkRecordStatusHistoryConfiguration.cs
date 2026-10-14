using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class WorkRecordStatusHistoryConfiguration : IEntityTypeConfiguration<WorkRecordStatusHistory>
{
    public void Configure(EntityTypeBuilder<WorkRecordStatusHistory> builder)
    {
        builder.ToTable("work_record_status_histories", table =>
        {
            table.HasCheckConstraint("ck_work_record_history_reason", "status NOT IN ('Failed','Cancelled') OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)");
            table.HasCheckConstraint("ck_work_record_history_record_id", "work_record_id > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("integer").ValueGeneratedOnAdd();
        builder.Property(x => x.WorkRecordType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.WorkRecordId).HasColumnType("integer").IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(200);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("timestamp with time zone").IsRequired();
        builder.HasIndex(x => new { x.WorkRecordType, x.WorkRecordId, x.Id });
    }
}
