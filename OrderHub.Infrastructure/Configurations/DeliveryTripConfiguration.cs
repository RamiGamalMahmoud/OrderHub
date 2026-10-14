using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class DeliveryTripConfiguration : ModelConfigurationBase<DeliveryTrip>
{
    public override void Configure(EntityTypeBuilder<DeliveryTrip> builder)
    {
        base.Configure(builder);
        builder.ToTable("delivery_trips", table =>
        {
            table.HasCheckConstraint("ck_delivery_trips_single_parent", "(order_id IS NOT NULL) <> (delivery_step_id IS NOT NULL)");
            table.HasCheckConstraint("ck_delivery_trips_numbering", "numbering_party_id > 0 AND work_record_number > 0 AND sequence_year > 0 AND sequence_month BETWEEN 1 AND 12");
        });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.NumberingPartyId).IsRequired();
        builder.Property(x => x.WorkRecordNumber).IsRequired();
        builder.Property(x => x.SequenceYear).IsRequired();
        builder.Property(x => x.SequenceMonth).IsRequired();
        builder.Ignore(x => x.StatusHistory);
        builder.Ignore("_statusHistory");
        builder.HasOne(x => x.Order).WithMany(x => x.DeliveryTrips).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DeliveryStep).WithMany(x => x.DeliveryTrips).HasForeignKey(x => x.DeliveryStepId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Deliveryman>().WithMany().HasForeignKey(x => x.NumberingPartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.DeliveryStepId);
        builder.HasIndex(x => new { x.NumberingPartyId, x.SequenceYear, x.SequenceMonth, x.WorkRecordNumber }).IsUnique();
    }
}
