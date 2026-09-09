using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OrderEntitySequenceConfiguration
    : ModelConfigurationBase<OrderEntitySequence>
{
    public override void Configure(EntityTypeBuilder<OrderEntitySequence> builder)
    {
        base.Configure(builder);

        builder.ToTable("order_entity_sequences");

        builder.Property(x => x.RecipientType)
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.SequenceYear)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.SequenceMonth)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.SequenceNumber)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.DisplayTitle)
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OrderId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.OrderId,
            x.RecipientType,
            x.EntityId
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.RecipientType,
            x.EntityId,
            x.SequenceYear,
            x.SequenceMonth,
            x.SequenceNumber
        })
        .IsUnique();
    }
}