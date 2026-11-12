using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class WarehouseConfiguration : ModelConfigurationBase<Warehouse>
{
    public override void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        base.Configure(builder);

        builder.ToTable("warehouses");

        builder.OwnsOne(warehouse => warehouse.Name, navigationBuilder =>
        {
            navigationBuilder.Property(entityName => entityName.Value)
                .HasColumnName("name")
                .HasColumnType("varchar(100)")
                .HasMaxLength(100)
                .IsRequired();

            navigationBuilder.HasIndex(entityName => entityName.Value)
                .IsUnique();
        });

        builder.Navigation(warehouse => warehouse.Name)
            .IsRequired();

        builder.Property(warehouse => warehouse.KeeperUserId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(warehouse => warehouse.IsActive)
            .HasColumnType("boolean")
            .IsRequired();

        builder.HasIndex(warehouse => warehouse.KeeperUserId)
            .IsUnique();

        builder.HasOne(warehouse => warehouse.Keeper)
            .WithMany()
            .HasForeignKey(warehouse => warehouse.KeeperUserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
