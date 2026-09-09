using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;
using System;

namespace OrderHub.Infrastructure.Configurations;

// ============================================================================
// Supplier
// ============================================================================

internal class SupplierConfiguration
    : ModelConfigurationBase<Supplier>
{
    public override void Configure(EntityTypeBuilder<Supplier> builder)
    {
        base.Configure(builder);

        builder.ToTable("suppliers");

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

        builder.Property<int?>("phone_id")
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.Phone)
            .WithMany()
            .HasForeignKey("phone_id")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property<int?>("address_id")
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.Address)
            .WithMany()
            .HasForeignKey("address_id")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property<int?>("whatsapp_group_id")
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.WhatsappGroup)
            .WithMany()
            .HasForeignKey("whatsapp_group_id")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(x => x.BusinessHours, navigationBuilder =>
        {
            navigationBuilder.Property(x => x.OpenAt)
                .HasColumnName("open_at")
                .HasColumnType("time")
                .IsRequired();

            navigationBuilder.Property(x => x.CloseAt)
                .HasColumnName("close_at")
                .HasColumnType("time")
                .IsRequired();
        });

        builder.Navigation(x => x.BusinessHours)
            .IsRequired(false);
    }
}