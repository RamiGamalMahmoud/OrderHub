using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Configurations;

internal class DeliverymanConfiguration
    : ModelConfigurationBase<Deliveryman>
{
    public override void Configure(EntityTypeBuilder<Deliveryman> builder)
    {
        base.Configure(builder);

        builder.ToTable("deliverymen");

        builder.OwnsOne(x => x.Name, navigationBuilder =>
        {
            navigationBuilder.Property(x => x.Value)
                .HasColumnName("name")
                .HasColumnType("varchar(100)")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Navigation(x => x.Name)
            .IsRequired();

        builder.Property<int>("deliveryman_city_id")
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.City)
            .WithMany()
            .HasForeignKey("deliveryman_city_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property<int?>("whatsapp_group_id")
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.WhatsappGroup)
            .WithMany()
            .HasForeignKey("whatsapp_group_id")
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(x => x.PhoneNumber)
            .HasColumnName("phone_number")
            .HasColumnType("varchar(15)")
            .HasMaxLength(15)
            .IsRequired(false);
    }
}