
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Infrastructure.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class NumberSequenceConfiguration
    : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.ToTable("number_sequences", table =>
        {
            table.HasCheckConstraint(
                "ck_number_sequences_kind",
                """
                kind IN ('Order', 'Supplier', 'Deliveryman', 'ShippingCarrier')
                """);

            table.HasCheckConstraint(
                "ck_number_sequences_scope",
                """
                (
                    kind = 'Order'
                    AND party_id = 0
                    AND year = 0
                    AND month = 0
                )
                OR
                (
                    kind IN ('Supplier', 'Deliveryman', 'ShippingCarrier')
                    AND party_id > 0
                    AND year > 0
                    AND month BETWEEN 1 AND 12
                )
                """);

            table.HasCheckConstraint(
                "ck_number_sequences_last_number",
                "last_number >= 0");
        });

        builder.HasKey(x => new
        {
            x.Kind,
            x.PartyId,
            x.Year,
            x.Month
        });

        builder.Property(x => x.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PartyId)
            .HasColumnName("party_id")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.Year)
            .HasColumnName("year")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.Month)
            .HasColumnName("month")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.LastNumber)
            .HasColumnName("last_number")
            .HasColumnType("bigint")
            .IsRequired();
    }
}
