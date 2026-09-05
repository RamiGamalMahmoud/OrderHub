using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OutboxMessageAttachmentConfiguration
    : ModelConfigurationBase<OutboxMessageAttachment>
{
    public override void Configure(EntityTypeBuilder<OutboxMessageAttachment> builder)
    {
        base.Configure(builder);

        builder.ToTable("message_attachments");

        builder.Property(x => x.OriginalFileName)
            .HasColumnType("varchar(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.StoredFileName)
            .HasColumnType("varchar(150)")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.RelativePath)
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasColumnType("varchar(100)")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.FileSize)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(x => x.OutboxMessageId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.OutboxMessage)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.OutboxMessageId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}