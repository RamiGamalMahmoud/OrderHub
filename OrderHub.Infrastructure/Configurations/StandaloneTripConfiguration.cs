using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models.Trips;

namespace OrderHub.Infrastructure.Configurations;

internal class StandaloneTripConfiguration : ModelConfigurationBase<StandaloneTrip>
{
    public override void Configure(EntityTypeBuilder<StandaloneTrip> builder)
    {
        base.Configure(builder);

        builder.ToTable(
            "standalone_trips",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_standalone_trips_status",
                    $"status IN ('{nameof(WorkRecordStatus.Pending)}', '{nameof(WorkRecordStatus.Completed)}', " +
                    $"'{nameof(WorkRecordStatus.Cancelled)}', '{nameof(WorkRecordStatus.Failed)}')");

                table.HasCheckConstraint(
                    "ck_standalone_trips_numbers",
                    "trip_number > 0 AND sequence_year > 0 AND sequence_month BETWEEN 1 AND 12");

                table.HasCheckConstraint(
                    "ck_standalone_trips_title_length",
                    "length(trim(title)) BETWEEN 1 AND 150");

                table.HasCheckConstraint(
                    "ck_standalone_trips_known_party",
                    $"(from_party_type = '{nameof(TripPartyType.External)}' OR from_party_id > 0) AND " +
                    $"(to_party_type = '{nameof(TripPartyType.External)}' OR to_party_id > 0)");

                table.HasCheckConstraint(
                    "ck_standalone_trips_external_party",
                    $"(from_party_type <> '{nameof(TripPartyType.External)}' OR " +
                    "(from_party_id IS NULL AND from_name IS NOT NULL AND from_phone IS NOT NULL AND from_address IS NOT NULL)) AND " +
                    $"(to_party_type <> '{nameof(TripPartyType.External)}' OR " +
                    "(to_party_id IS NULL AND to_name IS NOT NULL AND to_phone IS NOT NULL AND to_address IS NOT NULL))");
            });

        builder.Property(t => t.DeliverymanId)
            .HasColumnName("deliveryman_id")
            .IsRequired();

        builder.Property(t => t.Title)
            .HasColumnName("title")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        // From party (owned type)
        builder.OwnsOne(t => t.From, from =>
        {
            from.Property(p => p.PartyType)
                .HasColumnName("from_party_type")
                .HasMaxLength(30)
                .HasConversion<string>()
                .IsRequired();

            from.Property(p => p.PartyId)
                .HasColumnName("from_party_id");

            from.Property(p => p.Name)
                .HasColumnName("from_name")
                .HasMaxLength(100)
                .IsRequired();

            from.Property(p => p.Phone)
                .HasColumnName("from_phone")
                .HasMaxLength(30);

            from.Property(p => p.Address)
                .HasColumnName("from_address")
                .HasMaxLength(255);

            from.Property(p => p.Location)
                .HasColumnName("from_location")
                .HasMaxLength(255);
        });

        builder.Navigation(t => t.From).IsRequired();

        // To party (owned type)
        builder.OwnsOne(t => t.To, to =>
        {
            to.Property(p => p.PartyType)
                .HasColumnName("to_party_type")
                .HasMaxLength(30)
                .HasConversion<string>()
                .IsRequired();

            to.Property(p => p.PartyId)
                .HasColumnName("to_party_id");

            to.Property(p => p.Name)
                .HasColumnName("to_name")
                .HasMaxLength(100)
                .IsRequired();

            to.Property(p => p.Phone)
                .HasColumnName("to_phone")
                .HasMaxLength(30);

            to.Property(p => p.Address)
                .HasColumnName("to_address")
                .HasMaxLength(255);

            to.Property(p => p.Location)
                .HasColumnName("to_location")
                .HasMaxLength(255);
        });

        builder.Navigation(t => t.To).IsRequired();

        builder.Property(t => t.ScheduledAtUtc)
            .HasColumnName("scheduled_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasMaxLength(30)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(t => t.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .HasColumnType("timestamp with time zone");

        builder.Property(t => t.StatusChangedAtUtc)
            .HasColumnName("status_changed_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(t => t.TripNumber)
            .HasColumnName("trip_number")
            .IsRequired();

        builder.Property(t => t.SequenceYear)
            .HasColumnName("sequence_year")
            .IsRequired();

        builder.Property(t => t.SequenceMonth)
            .HasColumnName("sequence_month")
            .IsRequired();

        builder.Property(t => t.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .HasDefaultValue(1)
            .IsRequired();

        // Relationships
        builder.HasOne(t => t.Deliveryman)
            .WithMany()
            .HasForeignKey(t => t.DeliverymanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Attempts)
            .WithOne()
            .HasForeignKey("TripId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.History)
            .WithOne()
            .HasForeignKey("TripId")
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(t => new { t.DeliverymanId, t.SequenceYear, t.SequenceMonth, t.TripNumber })
            .IsUnique()
            .HasDatabaseName("uq_standalone_trips_number");

        builder.HasIndex(t => new { t.DeliverymanId, t.CreatedAt })
            .HasDatabaseName("ix_standalone_trips_deliveryman_created");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("ix_standalone_trips_created");
    }
}
