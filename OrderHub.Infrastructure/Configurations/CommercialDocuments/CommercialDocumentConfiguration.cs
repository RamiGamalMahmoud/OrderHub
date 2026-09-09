using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;

namespace OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal abstract class CommercialDocumentConfiguration<TDocument, TItem>
    : ModelConfigurationBase<TDocument>
    where TDocument : CommercialDocument<TItem>
    where TItem : CommercialDocumentItem
{
    public override void Configure(EntityTypeBuilder<TDocument> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.DocumentNumber)
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IssueDate)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CustomerName)
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CustomerPhone)
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CustomerAddress)
            .HasColumnType("varchar(500)")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Subtotal)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(x => x.TotalVat)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.HasIndex(x => x.DocumentNumber)
            .IsUnique();

        builder.Navigation(x => x.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}