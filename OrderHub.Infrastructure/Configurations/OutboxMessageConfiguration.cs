using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OutboxMessageConfiguration
    : ModelConfigurationBase<OutboxMessage>
{
    public override void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        base.Configure(builder);

        builder.ToTable("outbox_messages");

        builder.Property(x => x.Text)
            .HasColumnType("varchar(4000)")
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.RecipientType)
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.RetryCount)
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .IsRequired(false);

        builder.Property(x => x.MaxRetries)
            .HasColumnType("integer")
            .HasDefaultValue(3)
            .IsRequired(false);

        builder.Property(x => x.LastAttemptAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.SentAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(x => x.OrderId)
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.Order)
            .WithMany(x => x.OutboxMessages)
            .HasForeignKey(x => x.OrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.RecipientId)
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.Recipient)
            .WithMany()
            .HasForeignKey(x => x.RecipientId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(x => x.Notes)
            .HasColumnType("text[]")
            .IsRequired(false);

        builder.HasIndex(x => new
        {
            x.OrderId,
            x.RecipientId
        })
        .IsUnique()
        .HasFilter("status = 'Pending'");
    }
}