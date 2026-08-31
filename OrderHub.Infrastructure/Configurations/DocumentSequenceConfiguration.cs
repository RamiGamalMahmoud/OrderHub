using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Infrastructure.Models;

namespace OrderHub.Infrastructure.Configurations;

internal class DocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.HasKey(x => new
        {
            x.DocumentType,
            x.Year,
            x.Month
        });

        builder.Property(x => x.DocumentType).HasConversion<int>();

        builder.Property(x => x.Year).IsRequired();

        builder.Property(x => x.Month).IsRequired();

        builder.Property(x => x.LastNumber).IsRequired();
    }
}