using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;

namespace OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal abstract class CommercialDocumentConfiguration<TDocument, TItem>
    : ModelConfigurationBase<TDocument>
    where TDocument : CommercialDocument<TItem>
    where TItem : CommercialDocumentItem
{
    public override void Configure(
        EntityTypeBuilder<TDocument> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.DocumentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.IssueDate)
            .IsRequired();

        builder.Property(x => x.CustomerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.CustomerPhone)
            .HasMaxLength(50);

        builder.Property(x => x.Subtotal)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalVat)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.DocumentNumber)
            .IsUnique(true);

        builder.Navigation(x => x.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}