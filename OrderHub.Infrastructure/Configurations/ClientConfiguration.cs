using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class ClientConfiguration : ModelConfigurationBase<Client>
{
    public override void Configure(EntityTypeBuilder<Client> builder)
    {
        base.Configure(builder);

        builder.ToTable("clients");

        builder.OwnsOne(client => client.Name, navigationBuilder =>
        {
            navigationBuilder.Property(entityName => entityName.Value)
                .HasColumnName("name")
                .HasColumnType("varchar(100)")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Navigation(client => client.Name)
            .IsRequired();

        builder.Property(client => client.Location)
            .HasColumnName("location")
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property<int>("address_id")
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(client => client.Address)
            .WithMany()
            .HasForeignKey("address_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property<int>("phone_id")
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(client => client.Phone)
            .WithMany()
            .HasForeignKey("phone_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}