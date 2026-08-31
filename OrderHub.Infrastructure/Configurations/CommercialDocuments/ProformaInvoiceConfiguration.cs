using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;

namespace OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal sealed class ProformaInvoiceConfiguration
    : CommercialDocumentConfiguration<ProformaInvoice, ProformaInvoiceItem>
{
    public override void Configure(
        EntityTypeBuilder<ProformaInvoice> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.SourceDraftReference)
            .IsRequired(false);

        builder.HasIndex(x => x.SourceDraftReference)
            .IsUnique(false);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .IsRequired(false);

        builder.ToTable("proforma_invoices", table =>
        {
            table.HasCheckConstraint(
                "ck_proforma_invoice_source_reference_or_order",
                "source_draft_reference IS NOT NULL OR order_id IS NOT NULL");
        });

    }
}