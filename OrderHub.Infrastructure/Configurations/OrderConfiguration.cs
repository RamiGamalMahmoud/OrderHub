using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OrderConfiguration : ModelConfigurationBase<Order>
{
    public override void Configure(EntityTypeBuilder<Order> builder)
    {
        base.Configure(builder);

        builder.ToTable("orders");

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(o => o.ClientId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(o => o.Total);

        builder.HasMany(o => o.OrderItems)
            .WithOne()
            .HasForeignKey(o => o.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.OrderStatus)
            .WithMany()
            .HasForeignKey(o => o.OrderStatusId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
