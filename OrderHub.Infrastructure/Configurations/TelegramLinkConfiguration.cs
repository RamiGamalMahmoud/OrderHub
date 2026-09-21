using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class TelegramLinkConfiguration
    : ModelConfigurationBase<TelegramLink>
{
    public override void Configure(EntityTypeBuilder<TelegramLink> builder)
    {
        base.Configure(builder);

        builder.ToTable("telegram_links");

        builder.Property(x => x.OwnerType)
            .HasColumnName("owner_type")
            .HasColumnType("character varying(32)")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.OwnerId)
            .HasColumnName("owner_id")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.Token)
            .HasColumnName("token")
            .HasColumnType("character varying(64)")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UsedAt)
            .HasColumnName("used_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Ignore(x => x.IsUsed);

        builder.HasIndex(x => x.Token)
            .IsUnique()
            .HasDatabaseName("ux_telegram_links_token");

        builder.HasIndex(x => new
        {
            x.OwnerType,
            x.OwnerId
        })
        .HasDatabaseName("ix_telegram_links_owner");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("ix_telegram_links_expires_at");
    }
}