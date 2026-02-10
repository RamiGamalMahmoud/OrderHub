using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations
{
    internal class PhoneConfiguration : ModelConfigurationBase<Phone>
    {
        public override void Configure(EntityTypeBuilder<Phone> builder)
        {
            base.Configure(builder);

            builder.Property(phone => phone.IsPrimary)
                .IsRequired()
                .HasDefaultValue(false);

            builder.OwnsOne(phone => phone.Number, navigationBuilder =>
            {
                navigationBuilder.Property(number => number.NationalNumber)
                .HasColumnName("number")
                .HasColumnType("varchar(15)")
                .HasMaxLength(15)
                .IsRequired();

                navigationBuilder.Property(number => number.CountryCode)
                .HasColumnName("country_code")
                .HasColumnType("varchar(4)")
                .HasMaxLength(4)
                .IsRequired();

                navigationBuilder.Ignore(number => number.FullNumber);
            });
        }
    }
}
