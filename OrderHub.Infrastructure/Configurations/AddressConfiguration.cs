using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class AddressConfiguration : ModelConfigurationBase<Address>
{
    public override void Configure(EntityTypeBuilder<Address> builder)
    {
        base.Configure(builder);

        builder.ToTable("addresses");

        builder.Property(x => x.IsPrimary)
            .HasColumnType("boolean")
            .IsRequired();

        builder.Property(x => x.Street)
            .HasColumnName("street")
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property<int>("city_id")
            .HasColumnType("integer");

        builder.HasOne(x => x.City)
            .WithMany()
            .HasForeignKey("city_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}