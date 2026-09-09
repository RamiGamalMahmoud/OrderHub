using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;
using System.Collections.Generic;

namespace OrderHub.Infrastructure.Configurations;

internal class ProductConfiguration : ModelConfigurationBase<Product>
{
    public override void Configure(EntityTypeBuilder<Product> builder)
    {
        base.Configure(builder);

        builder.ToTable("products");

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

        builder.OwnsOne(x => x.Price, navigationBuilder =>
        {
            navigationBuilder.Property(x => x.Value)
                .HasColumnName("price")
                .HasColumnType("numeric(18,2)")
                .IsRequired();
        });

        builder.Navigation(x => x.Price)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasColumnType("varchar(10)")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property<int>("category_id")
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey("category_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Suppliers)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "product_supplier",
                right => right
                    .HasOne<Supplier>()
                    .WithMany()
                    .HasForeignKey("suppliers_id")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<Product>()
                    .WithMany()
                    .HasForeignKey("product_id")
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("product_supplier");

                    join.Property<int>("product_id")
                        .HasColumnType("integer")
                        .IsRequired();

                    join.Property<int>("suppliers_id")
                        .HasColumnType("integer")
                        .IsRequired();

                    join.HasKey("product_id", "suppliers_id");
                });

        builder.HasMany(x => x.Properties)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}