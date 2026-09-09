using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal class ProformaInvoiceItemConfiguration
    : CommercialDocumentItemConfiguration<ProformaInvoiceItem>
{
    public override void Configure(EntityTypeBuilder<ProformaInvoiceItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("proforma_invoice_items");

        builder.Property(x => x.ProformaInvoiceId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.ProformaInvoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.ProformaInvoiceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}