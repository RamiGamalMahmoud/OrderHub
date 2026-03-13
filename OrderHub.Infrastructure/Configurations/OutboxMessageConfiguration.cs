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
            .HasConversion<int>();

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(m => m.MaxRetries)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(m => m.LastAttemptAt)
            .HasColumnType("datetime2");

        builder.Property(m => m.SentAt)
            .HasColumnType("datetime2");

        builder.Property(m => m.ErrorMessage)
            .HasMaxLength(1000)
            .HasColumnType("nvarchar(1000)");

        builder.HasOne(m => m.Order)
            .WithMany(o => o.OutboxMessages)
            .HasForeignKey(m => m.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
