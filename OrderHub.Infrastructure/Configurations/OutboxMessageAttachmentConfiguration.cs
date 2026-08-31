using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class OutboxMessageAttachmentConfiguration : ModelConfigurationBase<OutboxMessageAttachment>
{
    public override void Configure(EntityTypeBuilder<OutboxMessageAttachment> builder)
    {
        base.Configure(builder);

        builder.Property(a => a.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.StoredFileName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.ContentType)
            .HasMaxLength(100);

        builder.Property(a => a.FileSize)
            .IsRequired();

        builder.HasOne(a => a.OutboxMessage)
            .WithMany(m => m.Attachments)
            .HasForeignKey(a => a.OutboxMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
