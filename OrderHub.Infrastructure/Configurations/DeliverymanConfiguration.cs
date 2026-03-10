using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations
{
    internal class DeliverymanConfiguration : ModelConfigurationBase<Deliveryman>
    {
        public override void Configure(EntityTypeBuilder<Deliveryman> builder)
        {
            base.Configure(builder);

            builder.OwnsOne(d => d.Name, navigationBuilder =>
            {
                navigationBuilder.Property(entityName => entityName.Value)
                .HasColumnName("name")
                .HasColumnType("VARCHAR(100)")
                .HasMaxLength(100)
                .IsRequired();
            });

            builder.Navigation(deliveryman => deliveryman.Name).IsRequired();

            builder.HasOne(deliveryman => deliveryman.City)
                .WithMany()
                .HasForeignKey("deliveryman_city_id")
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(d => d.PhoneNumber)
                .HasColumnType("varchar(15)")
                .HasMaxLength(15)
                .IsRequired(false);
        }
    }
}
