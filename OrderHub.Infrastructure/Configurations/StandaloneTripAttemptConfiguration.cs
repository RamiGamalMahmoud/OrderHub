using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models.Trips;

namespace OrderHub.Infrastructure.Configurations;

internal class StandaloneTripAttemptConfiguration : IEntityTypeConfiguration<StandaloneTripAttempt>
{
    public void Configure(EntityTypeBuilder<StandaloneTripAttempt> builder)
    {
        builder.ToTable(
            "standalone_trip_attempts",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_standalone_trip_attempts_number",
                    "attempt_number > 0");

                table.HasCheckConstraint(
                    "ck_standalone_trip_attempts_status",
                    $"status IN ('{nameof(WorkRecordStatus.Pending)}', '{nameof(WorkRecordStatus.Completed)}', " +
                    $"'{nameof(WorkRecordStatus.Cancelled)}', '{nameof(WorkRecordStatus.Failed)}')");

                table.HasCheckConstraint(
                    "ck_standalone_trip_attempts_pending_no_end",
                    $"status <> '{nameof(WorkRecordStatus.Pending)}' OR ended_at_utc IS NULL");

                table.HasCheckConstraint(
                    "ck_standalone_trip_attempts_terminal_has_end",
                    $"status = '{nameof(WorkRecordStatus.Pending)}' OR ended_at_utc IS NOT NULL");

                table.HasCheckConstraint(
                    "ck_standalone_trip_attempts_completed_started",
                    $"status NOT IN ('{nameof(WorkRecordStatus.Completed)}', '{nameof(WorkRecordStatus.Failed)}') " +
                    "OR started_at_utc IS NOT NULL");

                table.HasCheckConstraint(
                    "ck_standalone_trip_attempts_reason",
                    $"status NOT IN ('{nameof(WorkRecordStatus.Failed)}', '{nameof(WorkRecordStatus.Cancelled)}') " +
                    "OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)");

                table.HasCheckConstraint(
                    "ck_standalone_trip_attempts_timing",
                    "ended_at_utc IS NULL OR started_at_utc IS NULL OR ended_at_utc >= started_at_utc");
            });

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property(a => a.TripId)
            .HasColumnName("trip_id")
            .IsRequired();

        builder.Property(a => a.AttemptNumber)
            .HasColumnName("attempt_number")
            .IsRequired();

        builder.Property(a => a.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(a => a.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(a => a.EndedAtUtc)
            .HasColumnName("ended_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(a => a.Reason)
            .HasColumnName("reason")
            .HasMaxLength(200);

        builder.Property(a => a.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Indexes
        builder.HasIndex(a => new { a.TripId, a.AttemptNumber })
            .IsUnique()
            .HasDatabaseName("uq_standalone_trip_attempts_number");

        builder.HasIndex(a => a.TripId)
            .HasFilter($"status = '{nameof(WorkRecordStatus.Pending)}'")
            .IsUnique()
            .HasDatabaseName("uq_standalone_trip_attempts_open");

        builder.HasIndex(a => new { a.TripId, a.Status })
            .HasDatabaseName("ix_standalone_trip_attempts_status");
    }
}
