using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OrderConfiguration : ModelConfigurationBase<Order>
{
    public override void Configure(EntityTypeBuilder<Order> builder)
    {
        base.Configure(builder);

        builder.ToTable("orders");

        builder.Property(x => x.ClientId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.Client)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.ClientId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.OrderNumber)
            .HasColumnName("order_number")
            .HasColumnType("varchar(20)")
            .HasMaxLength(20)
            .IsRequired();

        builder.Ignore(x => x.Total);
        builder.Ignore(x => x.Pricing);

        builder.Property(x => x.DeliveryMethod)
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasConversion<string>()
            .IsRequired(false);

        builder.Property(x => x.OrderStatus)
            .HasColumnType("varchar(30)")
            .HasMaxLength(30)
            .HasConversion<string>()
            .HasDefaultValue(OrderStatus.Processing)
            .IsRequired();

        builder.Property(x => x.Version).IsConcurrencyToken().HasDefaultValue(1);
        builder.Property(x => x.ArchivedAtUtc).HasColumnType("timestamp with time zone");
        builder.Property(x => x.ArchiveReason).HasMaxLength(200);
        builder.Property(x => x.ArchivedByUserName).HasMaxLength(150);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.ArchivedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(x => x.CanEdit);
        builder.Ignore(x => x.IsFinal);
        builder.Ignore(x => x.CanArchive);
        builder.Ignore(x => x.CanDeliverDirectly);
        builder.HasMany(x => x.StatusHistory).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.StepHistory).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.IsArchived, x.CreatedAt });
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => x.CreationRequestId).IsUnique();
        builder.ToTable("orders", table => table.HasCheckConstraint("ck_orders_archive_metadata",
            "NOT is_archived OR (archived_at_utc IS NOT NULL AND archived_by_user_id IS NOT NULL AND archive_reason IS NOT NULL AND length(trim(archive_reason)) BETWEEN 1 AND 200)"));

        builder.Property(x => x.ShippingCarrierId)
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.ShippingCarrier)
            .WithMany()
            .HasForeignKey(x => x.ShippingCarrierId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.DeliverymanId)
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.Deliveryman)
            .WithMany()
            .HasForeignKey(x => x.DeliverymanId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.PaymentMethodId)
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.PaymentMethod)
            .WithMany()
            .HasForeignKey(x => x.PaymentMethodId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.OrderItems)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Supplies)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.DeliverySteps)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.OrderSource)
            .HasConversion<string>()
            .HasColumnName("order_source")
            .HasColumnType("varchar(30)")
            .IsRequired(false);
    }
}
