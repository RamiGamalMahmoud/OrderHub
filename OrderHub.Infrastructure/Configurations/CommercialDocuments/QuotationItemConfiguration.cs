using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;

namespace OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal sealed class QuotationItemConfiguration
    : CommercialDocumentItemConfiguration<QuotationItem>
{
    public override void Configure(
        EntityTypeBuilder<QuotationItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("quotation_items");

        builder.HasOne(x => x.Quotation)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}