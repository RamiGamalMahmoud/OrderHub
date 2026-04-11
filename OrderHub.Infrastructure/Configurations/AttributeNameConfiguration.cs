using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;
using System;

namespace OrderHub.Infrastructure.Configurations
{
    internal class AttributeNameConfiguration : ModelConfigurationBase<AttributeName>
    {
        public override void Configure(EntityTypeBuilder<AttributeName> builder)
        {
            base.Configure(builder);
            builder.ToTable("attribute_names");
            builder.Property(x => x.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(255);

            builder.HasIndex(x => x.Name).IsUnique();

            builder.HasData(
                [
                    new { Id = 1, Name = "القماش", CreatedAt = new DateTime(2026, 04, 11, 0, 0, 0, DateTimeKind.Utc) }, // "2026-04-11" },
                    new { Id = 2, Name = "الخشب", CreatedAt = new DateTime(2026, 04, 11, 0, 0, 0, DateTimeKind.Utc) },
                    new { Id = 3, Name = "الموديل", CreatedAt = new DateTime(2026, 04, 11, 0, 0, 0, DateTimeKind.Utc) },
                    new { Id = 4, Name = "اللون", CreatedAt = new DateTime(2026, 04, 11, 0, 0, 0, DateTimeKind.Utc) },
                    new { Id = 5, Name = "المقاس", CreatedAt = new DateTime(2026, 04, 11, 0, 0, 0, DateTimeKind.Utc) }
                ]);
        }
    }
}
