using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;
using System;

namespace OrderHub.Infrastructure.Configurations;

internal class PaymentMethodConfiguration
    : ModelConfigurationBase<PaymentMethod>
{
    public override void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        base.Configure(builder);

        builder.ToTable("payment_methods");

        builder.Property(x => x.Code)
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsUnicode(false)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.DisplayName)
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.DisplayName);

        builder.Property(x => x.Description)
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(x => x.IsActive);

        builder.HasData(
            new
            {
                Id = 1,
                Code = "ON_DELIVERY",
                DisplayName = "نقداً",
                Description = "الدفع نقداً عند الاستلام",
                IsActive = true,
                CreatedAt = new DateTime(
                    2026, 1, 1, 0, 0, 0,
                    DateTimeKind.Utc)
            },
            new
            {
                Id = 2,
                Code = "BANK_TRANSFER",
                DisplayName = "تحويل بنكي",
                Description = "الدفع عبر التحويل البنكي",
                IsActive = true,
                CreatedAt = new DateTime(
                    2026, 1, 1, 0, 0, 0,
                    DateTimeKind.Utc)
            },
            new
            {
                Id = 3,
                Code = "SUPPLIER_ACCOUNT",
                DisplayName = "على حساب المورد",
                Description = "الدفع على حساب المورد",
                IsActive = true,
                CreatedAt = new DateTime(
                    2026, 1, 1, 0, 0, 0,
                    DateTimeKind.Utc)
            },
            new
            {
                Id = 4,
                Code = "SALLA",
                DisplayName = "سلة",
                Description = "الدفع عبر منصة سلة",
                IsActive = true,
                CreatedAt = new DateTime(
                    2026, 1, 1, 0, 0, 0,
                    DateTimeKind.Utc)
            }
        );
    }
}