using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

// ============================================================================
// OutboxMessageRecipient
// ============================================================================

internal class RecipientConfiguration : ModelConfigurationBase<OutboxMessageRecipient>
{
    public override void Configure(EntityTypeBuilder<OutboxMessageRecipient> builder)
    {
        base.Configure(builder);

        builder.ToTable("outbox_message_recipients");

        builder.Property(x => x.Name)
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.PhoneNumber)
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .IsRequired();
    }
}


// ============================================================================
// ClientRecipient
// ============================================================================

internal class ClientRecipientConfiguration : ModelConfigurationBase<ClientRecipient>
{
    public override void Configure(EntityTypeBuilder<ClientRecipient> builder)
    {
        builder.ToTable("client_recipients");

        builder.Property(x => x.ClientId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}


// ============================================================================
// SupplierRecipient
// ============================================================================

internal class SupplierRecipientConfiguration : ModelConfigurationBase<SupplierRecipient>
{
    public override void Configure(EntityTypeBuilder<SupplierRecipient> builder)
    {
        builder.ToTable("supplier_recipients");

        builder.Property(x => x.SupplierId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}


// ============================================================================
// DeliverymanRecipient
// ============================================================================

internal class DeliverymanRecipientConfiguration : ModelConfigurationBase<DeliverymanRecipient>
{
    public override void Configure(EntityTypeBuilder<DeliverymanRecipient> builder)
    {
        builder.ToTable("deliveryman_recipients");

        builder.Property(x => x.DeliveryManId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.DeliveryMan)
            .WithMany()
            .HasForeignKey(x => x.DeliveryManId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}


// ============================================================================
// ShippingCarrierRecipient
// ============================================================================

internal class ShippingCarrierRecipientConfiguration
    : ModelConfigurationBase<ShippingCarrierRecipient>
{
    public override void Configure(
        EntityTypeBuilder<ShippingCarrierRecipient> builder)
    {
        builder.ToTable("shippingcarrier_recipients");

        builder.Property(x => x.ShippingCarrierId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.ShippingCarrier)
            .WithMany()
            .HasForeignKey(x => x.ShippingCarrierId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}