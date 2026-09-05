using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class ShippingCarrierConfiguration
    : ModelConfigurationBase<ShippingCarrier>
{
    public override void Configure(EntityTypeBuilder<ShippingCarrier> builder)
    {
        base.Configure(builder);

        builder.ToTable("shipping_carriers");

        builder.OwnsOne(x => x.Name, navigationBuilder =>
        {
            navigationBuilder.Property(x => x.Value)
                .HasColumnName("name")
                .HasColumnType("varchar(100)")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Navigation(x => x.Name)
            .IsRequired();

        builder.OwnsOne(x => x.ShippingCost, navigationBuilder =>
        {
            navigationBuilder.Property(x => x.Value)
                .HasColumnName("shipping_cost")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
        });

        builder.Navigation(x => x.ShippingCost)
            .IsRequired();

        builder.Property<int?>("address_id")
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.Address)
            .WithMany()
            .HasForeignKey("address_id")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property<int?>("phone_id")
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.Phone)
            .WithMany()
            .HasForeignKey("phone_id")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);
    }
}