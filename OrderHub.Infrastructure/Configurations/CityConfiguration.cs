using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class CityConfiguration : ModelConfigurationBase<City>
{
    public override void Configure(EntityTypeBuilder<City> builder)
    {
        builder.OwnsOne(city => city.Name,
            navigationBuilder =>
            {
                navigationBuilder.Property(entityName => entityName.Value)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

                navigationBuilder.HasIndex(entityName => entityName.Value).IsUnique();
            });

        builder.Navigation(city => city.Name).IsRequired();
    }
}
