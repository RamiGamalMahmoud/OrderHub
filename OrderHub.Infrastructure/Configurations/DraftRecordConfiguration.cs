using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;
using OrderHub.Infrastructure.Models;

namespace OrderHub.Infrastructure.Configurations;
internal class DraftRecordConfiguration : IEntityTypeConfiguration<DraftRecord>, IModelsConfigurationMarker
{
    public void Configure(EntityTypeBuilder<DraftRecord> builder)
    {
        builder.ToTable("order_drafts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Data)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.LastModified)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
