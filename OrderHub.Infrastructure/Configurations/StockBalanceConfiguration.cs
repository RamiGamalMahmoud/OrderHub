using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class StockBalanceConfiguration : ModelConfigurationBase<StockBalance>
{
    public override void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        base.Configure(builder);

        builder.ToTable("stock_balances", table =>
        {
            table.HasCheckConstraint("ck_stock_balances_stock_on_hand", "stock_on_hand >= 0");
            table.HasCheckConstraint("ck_stock_balances_reserved_on_hand", "reserved_on_hand >= 0");
            table.HasCheckConstraint("ck_stock_balances_damaged_qty", "damaged_qty >= 0");
            table.HasCheckConstraint("ck_stock_balances_reserved_le_stock", "reserved_on_hand <= stock_on_hand");
        });

        builder.Ignore(x => x.Available);

        builder.Property(x => x.WarehouseId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.StockOnHand)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.ReservedOnHand)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.DamagedQty)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.Version)
            .HasColumnType("integer")
            .IsRequired()
            .IsConcurrencyToken();

        builder.HasIndex(x => new { x.WarehouseId, x.ProductId }).IsUnique();

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
