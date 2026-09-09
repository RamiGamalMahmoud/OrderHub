using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal abstract class ModelConfigurationBase<TModel>
    : IEntityTypeConfiguration<TModel>
    where TModel : ModelBase
{
    public virtual void Configure(EntityTypeBuilder<TModel> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType("integer")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ModifiedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);
    }
}