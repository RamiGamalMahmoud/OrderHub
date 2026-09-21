using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class TelegramAccountConfiguration
    : ModelConfigurationBase<TelegramAccount>
{
    public override void Configure(EntityTypeBuilder<TelegramAccount> builder)
    {
        base.Configure(builder);

        builder.ToTable("telegram_accounts");

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

        builder.Property(x => x.TelegramUserId)
            .HasColumnName("telegram_user_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(x => x.ChatId)
            .HasColumnName("chat_id")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(x => x.Username)
            .HasColumnName("username")
            .HasColumnType("character varying(64)")
            .HasMaxLength(64)
            .IsRequired(false);

        builder.Property(x => x.FirstName)
            .HasColumnName("first_name")
            .HasColumnType("character varying(128)")
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.LastName)
            .HasColumnName("last_name")
            .HasColumnType("character varying(128)")
            .HasMaxLength(128)
            .IsRequired(false);

        builder.Property(x => x.LinkedAt)
            .HasColumnName("linked_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.OwnerType,
            x.OwnerId
        })
        .IsUnique()
        .HasDatabaseName("ux_telegram_accounts_owner");

        builder.HasIndex(x => x.TelegramUserId)
            .IsUnique()
            .HasDatabaseName("ux_telegram_accounts_telegram_user_id");

        builder.HasIndex(x => x.ChatId)
            .IsUnique()
            .HasDatabaseName("ux_telegram_accounts_chat_id");
    }
}