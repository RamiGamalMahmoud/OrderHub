using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

// ============================================================================
// WhatsappGroup
// ============================================================================

internal class WhatsappGroupConfiguration
    : ModelConfigurationBase<WhatsappGroup>
{
    public override void Configure(EntityTypeBuilder<WhatsappGroup> builder)
    {
        base.Configure(builder);

        builder.ToTable("whatsapp_groups");

        builder.Property(x => x.GroupName)
            .HasColumnName("group_name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.GroupLink)
            .HasColumnName("group_link")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.GroupType)
            .HasColumnName("group_type")
            .HasColumnType("text")
            .HasConversion<string>()
            .IsRequired();
    }
}