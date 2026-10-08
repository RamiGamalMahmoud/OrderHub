using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class PropertyOptionConfiguration
    : ModelConfigurationBase<PropertyOption>
{
    public override void Configure(EntityTypeBuilder<PropertyOption> builder)
    {
        base.Configure(builder);

        builder.ToTable("property_options");

        builder.Property(x => x.PropertyId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.Value)
            .HasColumnName("value")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(x => x.Property)
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.PropertyId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.PropertyId,
            x.Value
        })
        .IsUnique();
    }
}