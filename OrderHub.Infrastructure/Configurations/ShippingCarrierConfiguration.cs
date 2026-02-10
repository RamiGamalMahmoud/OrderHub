using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class ShippingCarrierConfiguration : ModelConfigurationBase<ShippingCarrier>
{
    public override void Configure(EntityTypeBuilder<ShippingCarrier> builder)
    {
        builder.OwnsOne(product => product.Name,
            navigationBuilder =>
            {
                navigationBuilder.Property(entityName => entityName.Value)
                .HasColumnName("name")
                .HasColumnType("VARCHAR(100)")
                .HasMaxLength(100)
                .IsRequired();
            });

        builder.Navigation(s => s.Name).IsRequired();

        builder.OwnsOne(s => s.ShippingCost, shipingCostNavigation => shipingCostNavigation.Property(shippingCoast => shippingCoast.Value)
            .HasColumnName("shipping_cost")
            .HasColumnType("DECIMAL(18,2)")
            .IsRequired());

        builder.Navigation(s => s.ShippingCost).IsRequired();

        builder.HasOne(s => s.Address).WithMany().HasForeignKey("address_id");

        builder.HasOne(s => s.Phone).WithMany().HasForeignKey("phone_id");
    }
}
