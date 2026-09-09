using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;

namespace OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal class InvoiceConfiguration
    : CommercialDocumentConfiguration<Invoice, InvoiceItem>
{
    public override void Configure(EntityTypeBuilder<Invoice> builder)
    {
        base.Configure(builder);

        builder.ToTable("invoices");

        builder.Property(x => x.OrderId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.Order)
            .WithOne()
            .HasForeignKey<Invoice>(x => x.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}