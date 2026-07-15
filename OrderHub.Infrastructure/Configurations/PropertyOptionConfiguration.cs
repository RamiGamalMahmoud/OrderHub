using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class PropertyOptionConfiguration : ModelConfigurationBase<PropertyOption>
{
    public override void Configure(EntityTypeBuilder<PropertyOption> builder)
    {
        base.Configure(builder);
        builder.Property(x => x.Value)
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.HasOne(x => x.Property)
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.PropertyId,
            x.Value
        }).IsUnique();
    }
}
