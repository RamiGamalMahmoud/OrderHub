using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;

namespace OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal sealed class InvoiceConfiguration
    : CommercialDocumentConfiguration<Invoice, InvoiceItem>
{
    public override void Configure(
        EntityTypeBuilder<Invoice> builder)
    {
        base.Configure(builder);

        builder.HasIndex(x => x.DocumentNumber)
            .IsUnique();

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.HasOne(x => x.Order)
            .WithOne()
            .HasForeignKey<Invoice>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}