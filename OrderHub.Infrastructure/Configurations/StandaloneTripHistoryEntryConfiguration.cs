using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using OrderHub.Domain.Models.Trips;

namespace OrderHub.Infrastructure.Configurations;

internal class StandaloneTripHistoryEntryConfiguration : IEntityTypeConfiguration<StandaloneTripHistoryEntry>
{
    public void Configure(EntityTypeBuilder<StandaloneTripHistoryEntry> builder)
    {
        builder.ToTable(
            "standalone_trip_histories",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_standalone_trip_histories_event_type",
                    $"event_type IN ('{nameof(StandaloneTripEventType.Created)}', " +
                    $"'{nameof(StandaloneTripEventType.AttemptStarted)}', " +
                    $"'{nameof(StandaloneTripEventType.Completed)}', " +
                    $"'{nameof(StandaloneTripEventType.Failed)}', " +
                    $"'{nameof(StandaloneTripEventType.Cancelled)}', " +
                    $"'{nameof(StandaloneTripEventType.Retried)}', " +
                    $"'{nameof(StandaloneTripEventType.Reassigned)}', " +
                    $"'{nameof(StandaloneTripEventType.Edited)}')");

                table.HasCheckConstraint(
                    "ck_standalone_trip_histories_reason",
                    $"event_type NOT IN ('{nameof(StandaloneTripEventType.Failed)}', " +
                    $"'{nameof(StandaloneTripEventType.Cancelled)}', " +
                    $"'{nameof(StandaloneTripEventType.Reassigned)}') " +
                    "OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)");

                table.HasCheckConstraint(
                    "ck_standalone_trip_histories_reassignment",
                    $"event_type <> '{nameof(StandaloneTripEventType.Reassigned)}' OR " +
                    "(previous_deliveryman_id > 0 AND new_deliveryman_id > 0 " +
                    "AND previous_deliveryman_name IS NOT NULL AND new_deliveryman_name IS NOT NULL " +
                    "AND previous_trip_number > 0 AND new_trip_number > 0)");
            });

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");

        builder.Property(h => h.TripId)
            .HasColumnName("trip_id")
            .IsRequired();

        builder.Property(h => h.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(30)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(h => h.AttemptNumber)
            .HasColumnName("attempt_number");

        builder.Property(h => h.PreviousStatus)
            .HasColumnName("previous_status")
            .HasMaxLength(30)
            .HasConversion<string>();

        builder.Property(h => h.NewStatus)
            .HasColumnName("new_status")
            .HasMaxLength(30)
            .HasConversion<string>();

        builder.Property(h => h.PreviousDeliverymanId)
            .HasColumnName("previous_deliveryman_id");

        builder.Property(h => h.NewDeliverymanId)
            .HasColumnName("new_deliveryman_id");

        builder.Property(h => h.PreviousDeliverymanName)
            .HasColumnName("previous_deliveryman_name")
            .HasMaxLength(100);

        builder.Property(h => h.NewDeliverymanName)
            .HasColumnName("new_deliveryman_name")
            .HasMaxLength(100);

        builder.Property(h => h.PreviousTripNumber)
            .HasColumnName("previous_trip_number");

        builder.Property(h => h.NewTripNumber)
            .HasColumnName("new_trip_number");

        builder.Property(h => h.Reason)
            .HasColumnName("reason")
            .HasMaxLength(200);

        builder.Property(h => h.ChangedByUserId)
            .HasColumnName("changed_by_user_id")
            .IsRequired();

        builder.Property(h => h.ChangedByUserName)
            .HasColumnName("changed_by_user_name")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(h => h.ChangedAtUtc)
            .HasColumnName("changed_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(h => h.Source)
            .HasColumnName("source")
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(h => h.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(h => new { h.TripId, h.Id })
            .HasDatabaseName("ix_standalone_trip_histories_trip");

        builder.HasIndex(h => h.PreviousDeliverymanId)
            .HasDatabaseName("ix_standalone_trip_histories_prev_deliveryman");

        builder.HasIndex(h => h.NewDeliverymanId)
            .HasDatabaseName("ix_standalone_trip_histories_new_deliveryman");
    }
}
