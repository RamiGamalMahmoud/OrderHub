using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal class ProformaInvoiceConfiguration
    : CommercialDocumentConfiguration<ProformaInvoice, ProformaInvoiceItem>
{
    public override void Configure(EntityTypeBuilder<ProformaInvoice> builder)
    {
        base.Configure(builder);

        builder.ToTable("proforma_invoices", table =>
        {
            table.HasCheckConstraint(
                "ck_proforma_invoice_source_reference_or_order",
                "source_draft_reference IS NOT NULL OR order_id IS NOT NULL");
        });

        builder.Property(x => x.SourceDraftReference)
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(x => x.OrderId)
            .HasColumnType("integer")
            .IsRequired(false);

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}