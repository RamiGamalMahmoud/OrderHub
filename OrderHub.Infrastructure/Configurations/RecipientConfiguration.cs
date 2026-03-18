using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class RecipientConfiguration : ModelConfigurationBase<OutboxMessageRecipient>
{
    public override void Configure(EntityTypeBuilder<OutboxMessageRecipient> builder)
    {
        base.Configure(builder);

        builder.ToTable("outbox_message_recipients");

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(30)
            .IsRequired();
    }
}

internal class ClientRecipientConfiguration : ModelConfigurationBase<ClientRecipient>
{
    public override void Configure(EntityTypeBuilder<ClientRecipient> builder)
    {
        builder.ToTable("client_recipients");
        builder.HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal class SupplierRecipientConfiguration : ModelConfigurationBase<SupplierRecipient>
{
    public override void Configure(EntityTypeBuilder<SupplierRecipient> builder)
    {
        builder.ToTable("supplier_recipients");
        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal class DeliverymanRecipientConfiguration : ModelConfigurationBase<DeliverymanRecipient>
{
    public override void Configure(EntityTypeBuilder<DeliverymanRecipient> builder)
    {
        builder.ToTable("deliveryman_recipients");
        
        builder.HasOne(x => x.DeliveryMan)
            .WithMany()
            .HasForeignKey(x => x.DeliveryManId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal class ShippingCarrierRecipientConfiguration : ModelConfigurationBase<ShippingCarrierRecipient>
{
    public override void Configure(EntityTypeBuilder<ShippingCarrierRecipient> builder)
    {
        builder.ToTable("shippingcarrier_recipients");
        builder.HasOne(x => x.ShippingCarrier)
            .WithMany()
            .HasForeignKey(x => x.ShippingCarrierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
