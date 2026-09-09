using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class CategoryConfiguration : ModelConfigurationBase<Category>
{
    public override void Configure(EntityTypeBuilder<Category> builder)
    {
        base.Configure(builder);

        builder.ToTable("categories");

        builder.OwnsOne(category => category.Name, navigationBuilder =>
        {
            navigationBuilder.Property(entityName => entityName.Value)
                .HasColumnName("name")
                .HasColumnType("varchar(100)")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Navigation(category => category.Name)
            .IsRequired();

        builder.Property(category => category.ParentCategoryId)
            .HasColumnName("parent_category_id")
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(category => category.ParentCategory)
            .WithMany(category => category.SubCategories)
            .HasForeignKey(category => category.ParentCategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}