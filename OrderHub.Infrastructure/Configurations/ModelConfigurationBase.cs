using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations
{
    internal abstract class ModelConfigurationBase<TModel> : IEntityTypeConfiguration<TModel> where TModel : ModelBase
    {
        public virtual void Configure(EntityTypeBuilder<TModel> builder)
        {
            builder.HasKey(tModel => tModel.Id);
            builder.Property(tModel => tModel.CreatedAt).IsRequired();

            builder.Property(tModel => tModel.ModifiedAt).IsRequired(false);
        }
    }
}
