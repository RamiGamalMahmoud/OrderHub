using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations
{
    internal class ClientConfiguration : ModelConfigurationBase<Client>
    {
        public override void Configure(EntityTypeBuilder<Client> builder)
        {
            base.Configure(builder);

            builder.OwnsOne(client => client.Name, navigationBuilder =>
            {
                navigationBuilder.Property(entityName => entityName.Value)
                    .HasColumnName("name")
                    .HasColumnType("VARCHAR(100)")
                    .HasMaxLength(200)
                    .IsRequired();
            });

            builder.Property(c => c.Location).HasColumnType("VARCHAR(255)").IsRequired(false);

            builder.HasOne(client => client.Address).WithMany().HasForeignKey("address_id");

            builder.HasOne(client => client.Phone).WithMany().HasForeignKey("phone_id");

        }
    }
}
