using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OrderEntitySequenceConfiguration : ModelConfigurationBase<OrderEntitySequence>
{
    public override void Configure(EntityTypeBuilder<OrderEntitySequence> builder)
    {
        base.Configure(builder);

        builder.ToTable("order_entity_sequences");

        builder.Property(sequence => sequence.RecipientType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(sequence => sequence.EntityId)
            .IsRequired();

        builder.Property(sequence => sequence.SequenceYear)
            .IsRequired();

        builder.Property(sequence => sequence.SequenceMonth)
            .IsRequired();

        builder.Property(sequence => sequence.SequenceNumber)
            .IsRequired();

        builder.Property(sequence => sequence.DisplayTitle)
            .HasColumnType("VARCHAR(50)")
            .IsRequired();

        builder.HasIndex(sequence => new
            {
                sequence.OrderId,
                sequence.RecipientType,
                sequence.EntityId
            })
            .IsUnique();

        builder.HasIndex(sequence => new
            {
                sequence.RecipientType,
                sequence.EntityId,
                sequence.SequenceYear,
                sequence.SequenceMonth,
                sequence.SequenceNumber
            })
            .IsUnique();
    }
}
