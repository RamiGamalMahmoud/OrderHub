using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;
using System;

namespace OrderHub.Infrastructure.Configurations
{
    internal class SupplierConfiguration : ModelConfigurationBase<Supplier>
    {
        public override void Configure(EntityTypeBuilder<Supplier> builder)
        {
            base.Configure(builder);

            builder.OwnsOne(supplier => supplier.Name, navigationBuilder =>
            {
                navigationBuilder.Property(name => name.Value)
                .HasColumnName("name")
                .HasColumnType("varchar(100)")
                .HasMaxLength(100)
                .IsRequired();
            });

            builder.Navigation(supplier => supplier.Name).IsRequired();

            builder.HasOne(supplier => supplier.Address).WithMany().HasForeignKey("address_id");

            builder.HasOne(supplier => supplier.Phone).WithMany().HasForeignKey("phone_id");

            builder.OwnsOne(supplier => supplier.BusinessHours, navigationBuilder =>
            {
                navigationBuilder.Property(businessHours => businessHours.OpenAt)
                .HasColumnType("time")
                .HasColumnName("open_at")
                .HasConversion(
                    timeOnly => timeOnly.ToTimeSpan(), 
                    timeSpan => TimeOnly.FromTimeSpan(timeSpan));

                navigationBuilder.Property(businessHours => businessHours.CloseAt)
                .HasColumnType("time")
                .HasColumnName("close_at")
                .HasConversion(
                    timeOnly => timeOnly.ToTimeSpan(),
                    timeSpan => TimeOnly.FromTimeSpan(timeSpan));
            });
        }
    }
}
