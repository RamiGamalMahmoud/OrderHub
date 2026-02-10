using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;
using System;

namespace OrderHub.Infrastructure.Configurations
{
    internal class OrderStatusConfiguration : ModelConfigurationBase<OrderStatus>
    {
        public override void Configure(EntityTypeBuilder<OrderStatus> builder)
        {
            DateTime createdAt = new DateTime(2026, 2, 8);
            base.Configure(builder);

            builder.Property(o => o.Status)
                .HasColumnType("VARCHAR(40)")
                .IsRequired();

            builder.Property(o => o.DisplayName)
                .HasColumnType("VARCHAR(50)")
                .IsRequired();

            builder.HasData(
            [
                new { Id = 1, Status = "Pending", DisplayName = "تحت المراجعة" , CreatedAt = createdAt},
                new { Id = 2, Status = "Processing", DisplayName = "قيد التنفيذ", CreatedAt = createdAt},
                new { Id = 3, Status = "Shipped", DisplayName = "تم الشحن", CreatedAt = createdAt},
                new { Id = 4, Status = "Delivered", DisplayName = "تم التوصيل", CreatedAt = createdAt},
                new { Id = 5, Status = "Cancelled", DisplayName = "ملغي" , CreatedAt = createdAt}
            ]);
        }
    }
}
