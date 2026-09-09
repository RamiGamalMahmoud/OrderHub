using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class PhoneConfiguration : ModelConfigurationBase<Phone>
{
    public override void Configure(EntityTypeBuilder<Phone> builder)
    {
        base.Configure(builder);

        builder.ToTable("phones");

        builder.Property(x => x.IsPrimary)
            .HasColumnType("boolean")
            .IsRequired()
            .HasDefaultValue(false);

        builder.OwnsOne(x => x.Number, navigationBuilder =>
        {
            navigationBuilder.Property(x => x.NationalNumber)
                .HasColumnName("number")
                .HasColumnType("varchar(15)")
                .HasMaxLength(15)
                .IsRequired();

            navigationBuilder.Property(x => x.CountryCode)
                .HasColumnName("country_code")
                .HasColumnType("varchar(4)")
                .HasMaxLength(4)
                .IsRequired();

            navigationBuilder.Ignore(x => x.FullNumber);
        });

        builder.Navigation(x => x.Number)
            .IsRequired();
    }
}