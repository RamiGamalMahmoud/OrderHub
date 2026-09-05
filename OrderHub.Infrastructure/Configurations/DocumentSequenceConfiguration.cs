using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Infrastructure.Models;

internal class DocumentSequenceConfiguration
    : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.ToTable("document_sequences");

        builder.HasKey(x => new
        {
            x.DocumentType,
            x.Year,
            x.Month
        });

        builder.Property(x => x.DocumentType)
            .HasColumnType("integer")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Year)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.Month)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.LastNumber)
            .HasColumnType("integer")
            .IsRequired();
    }
}