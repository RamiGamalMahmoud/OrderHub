using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OrderItemPropertyConfiguration : ModelConfigurationBase<OrderItemProperty>
{
    public override void Configure(EntityTypeBuilder<OrderItemProperty> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.Value)
            .HasColumnType("varchar(100)")
            .IsRequired(false);

        builder.HasOne(x => x.OrderItem)
            .WithMany(x => x.Properties)
            .HasForeignKey(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(x => x.Property)
            .WithMany()
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasIndex(x => new { x.OrderItemId, x.PropertyId }).IsUnique();
    }
}
