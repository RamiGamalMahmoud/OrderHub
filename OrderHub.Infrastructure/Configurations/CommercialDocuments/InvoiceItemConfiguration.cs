using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;

namespace OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal class InvoiceItemConfiguration
    : CommercialDocumentItemConfiguration<InvoiceItem>
{
    public override void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("invoice_items");

        builder.Property(x => x.InvoiceId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.Invoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.InvoiceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}