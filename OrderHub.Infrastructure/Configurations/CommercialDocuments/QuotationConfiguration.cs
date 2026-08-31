using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;

namespace OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal sealed class QuotationConfiguration
    : CommercialDocumentConfiguration<Quotation, QuotationItem>
{
    public override void Configure(EntityTypeBuilder<Quotation> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ValidUntil)
            .IsRequired();

        builder.Property(x => x.SourceDraftReference)
            .IsRequired(false);

        builder.HasIndex(x => x.SourceDraftReference)
            .IsUnique(false);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .IsRequired(false);

        builder.ToTable("quotations", table =>
        {
            table.HasCheckConstraint(
                "ck_quotation_source_reference_or_order",
                "source_draft_reference IS NOT NULL OR order_id IS NOT NULL");
        });
    }
}