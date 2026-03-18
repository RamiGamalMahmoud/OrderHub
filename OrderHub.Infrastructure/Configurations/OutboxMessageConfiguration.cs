using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OutboxMessageConfiguration : ModelConfigurationBase<OutboxMessage>
{
    public override void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        base.Configure(builder);

        builder.Property(m => m.Text)
            .IsRequired()
            .HasMaxLength(4000)
            .HasColumnType("nvarchar(4000)");

        builder.Property(m => m.RecipientType)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(m => m.RetryCount)
            .IsRequired(false)
            .HasDefaultValue(0);

        builder.Property(m => m.MaxRetries)
            .IsRequired(false)
            .HasDefaultValue(3);

        builder.Property(m => m.LastAttemptAt)
            .HasColumnType("datetime2");

        builder.Property(m => m.SentAt)
            .HasColumnType("datetime2");

        builder.HasOne(m => m.Order)
            .WithMany(o => o.OutboxMessages)
            .HasForeignKey(m => m.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Recipient)
            .WithMany()
            .HasForeignKey(m => m.RecipientId)
            .IsRequired(false);

        builder.HasIndex(e => new { e.OrderId, e.RecipientId })
          .IsUnique()
          .HasFilter("[Status] = 'Pending'");
    }
}
