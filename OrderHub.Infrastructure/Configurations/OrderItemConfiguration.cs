using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;
using System.Numerics;

namespace OrderHub.Infrastructure.Configurations
{
    internal class OrderItemConfiguration : ModelConfigurationBase<OrderItem>
    {
        public override void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            base.Configure(builder);

            builder.ToTable("order_items");

            builder.OwnsOne(o => o.UnitPrice, navigationBuilder => navigationBuilder.Property(unitPrice => unitPrice.Value)
                .HasColumnName("unit_price")
                .HasColumnType("DECIMAL(18,2)")
                .IsRequired());

            builder.Navigation(o => o.UnitPrice).IsRequired();

            builder.Property(o => o.Quantity)
                .HasColumnName("quantity")
                .HasColumnType("DECIMAL(18,2)")
                .IsRequired();

            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();

            builder.Property(o => o.ProductName)
                .HasColumnName("product_name")
                .HasColumnType("VARCHAR(100)")
                .HasMaxLength(100)
                .IsRequired();

            builder.HasOne(o => o.Supplier)
                .WithMany()
                .HasForeignKey(o => o.SupplierId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.Property(o => o.SupplierName)
                .HasColumnName("supplier_name")
                .HasColumnType("VARCHAR(100)")
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Ignore(o => o.SubTotal);
        }
    }
}
