using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;
using OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal class QuotationItemConfiguration
    : CommercialDocumentItemConfiguration<QuotationItem>
{
    public override void Configure(EntityTypeBuilder<QuotationItem> builder)
    {
        base.Configure(builder);

        builder.ToTable("quotation_items");

        builder.Property(x => x.QuotationId)
            .HasColumnType("integer")
            .IsRequired();

        builder.HasOne(x => x.Quotation)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.QuotationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}