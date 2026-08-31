using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;

namespace OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal sealed class InvoiceItemConfiguration
    : CommercialDocumentItemConfiguration<InvoiceItem>
{
    public override void Configure(
        EntityTypeBuilder<InvoiceItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("invoice_items");

        builder.HasOne(x => x.Invoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}