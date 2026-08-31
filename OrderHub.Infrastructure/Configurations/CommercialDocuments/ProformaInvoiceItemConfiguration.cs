using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;

namespace OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal sealed class ProformaInvoiceItemConfiguration
    : CommercialDocumentItemConfiguration<ProformaInvoiceItem>
{
    public override void Configure(
        EntityTypeBuilder<ProformaInvoiceItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("proforma_invoice_items");

        builder.HasOne(x => x.ProformaInvoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ProformaInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}