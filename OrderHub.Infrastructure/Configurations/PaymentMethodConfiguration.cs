using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;
using System;

namespace OrderHub.Infrastructure.Configurations;

internal class PaymentMethodConfiguration : ModelConfigurationBase<PaymentMethod>
{
    public override void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        base.Configure(builder);

        builder.Property(p => p.Id)
            .ValueGeneratedOnAdd();
        // Code configuration
        builder.Property(p => p.Code)
            .IsRequired() // NOT NULL
            .HasMaxLength(50)
            .IsUnicode(false); // For codes, use varchar (better performance)

        // Unique index on Code
        builder.HasIndex(p => p.Code)
            .IsUnique()
            .HasDatabaseName("UX_PaymentMethods_Code");

        // DisplayName configuration
        builder.Property(p => p.DisplayName)
            .IsRequired() // NOT NULL
            .HasMaxLength(100)
            .IsUnicode(true); // For Arabic/Unicode support

        // Index on DisplayName for searching
        builder.HasIndex(p => p.DisplayName)
            .HasDatabaseName("IX_PaymentMethods_DisplayName");

        // Description configuration
        builder.Property(p => p.Description)
            .IsRequired(false) // NULL allowed
            .HasMaxLength(500)
            .IsUnicode(true);

        // IsActive configuration
        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true); // Default value = true

        // Index on IsActive for filtering
        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_PaymentMethods_IsActive");

        // Seed initial data
        builder.HasData(
            new
            {
                Id = 1,
                Code = "ON_DELIVERY",
                DisplayName = "نقداً",
                Description = "الدفع نقداً عند الاستلام",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0)
            },
            new
            {
                Id = 2,
                Code = "BANK_TRANSFER",
                DisplayName = "تحويل بنكي",
                Description = "الدفع عبر التحويل البنكي",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0)
            },
            new
            {
                Id = 3,
                Code = "SUPPLIER_ACCOUNT",
                DisplayName = "على حساب المورد",
                Description = "الدفع على حساب المورد",
                IsActive = true, 
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0)
            },
            new
            {
                Id = 4,
                Code = "SALLA",
                DisplayName = "سلة",
                Description = "الدفع عبر منصة سلة",
                IsActive = true,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0)
            }
        );
    }
}
