using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations
{
    internal class CategoryConfiguration : ModelConfigurationBase<Category>
    {
        public override void Configure(EntityTypeBuilder<Category> builder)
        {
            base.Configure(builder);

            builder.OwnsOne(category => category.Name,
                navigationBuilder =>
                {
                    navigationBuilder.Property(entityName => entityName.Value)
                    .HasColumnName("name")
                    .HasMaxLength(100)
                    .IsRequired();
                });

            builder.Navigation(category => category.Name).IsRequired();

            builder.HasOne(category => category.ParentCategory)
                .WithMany(category => category.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
        }
    }
}
