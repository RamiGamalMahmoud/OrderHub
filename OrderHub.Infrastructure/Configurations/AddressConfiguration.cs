using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations
{
    internal class AddressConfiguration : ModelConfigurationBase<Address>
    {
        public override void Configure(EntityTypeBuilder<Address> builder)
        {
            base.Configure(builder);
            builder.HasKey(address => address.Id);

            builder.Property(address => address.IsPrimary)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(address => address.Street)
                .HasColumnType("varchar(150)")
                .HasMaxLength(150)
                .IsRequired();

            builder.HasOne(address => address.City)
                .WithMany()
                .HasForeignKey("city_id")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
