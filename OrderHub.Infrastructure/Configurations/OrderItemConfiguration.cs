using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OrderItemConfiguration : ModelConfigurationBase<OrderItem>
{
    public override void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("order_items");

        builder.Property(x => x.OrderId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnType("integer")
            .IsRequired();

        builder.OwnsOne(x => x.UnitPrice, navigationBuilder =>
        {
            navigationBuilder.Property(unitPrice => unitPrice.Value)
                .HasColumnName("unit_price")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
        });

        builder.Navigation(x => x.UnitPrice)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.ProductName)
            .HasColumnName("product_name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.SupplierId)
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.SupplierName)
            .HasColumnName("supplier_name")
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Ignore(x => x.SubTotal);
    }
}