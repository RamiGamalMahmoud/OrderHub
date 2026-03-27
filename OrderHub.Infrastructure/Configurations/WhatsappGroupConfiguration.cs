using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class WhatsappGroupConfiguration : ModelConfigurationBase<WhatsappGroup>
{
    public override void Configure(EntityTypeBuilder<WhatsappGroup> builder)
    {
        base.Configure(builder);

        builder.ToTable("whatsapp_groups");

        builder.Property(w => w.GroupName)
            .HasColumnType("VARCHAR(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(w => w.GroupType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.GroupLink)
            .HasColumnType("VARCHAR(100)")
            .IsRequired();
    }
}
