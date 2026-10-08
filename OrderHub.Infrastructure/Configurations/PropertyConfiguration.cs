using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class PropertyConfiguration : ModelConfigurationBase<Property>
{
    public override void Configure(EntityTypeBuilder<Property> builder)
    {
        base.Configure(builder);

        builder.ToTable("properties");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.Name)
            .IsUnique();

        builder.Property(x => x.PropertyType)
            .HasColumnName("property_type")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.HasMany(x => x.Options)
            .WithOne(x => x.Property)
            .HasForeignKey(x => x.PropertyId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}