using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models.CommercialDocuments;

namespace OrderHub.Infrastructure.Configurations.CommercialDocuments;

internal abstract class CommercialDocumentItemConfiguration<TItem>
    : ModelConfigurationBase<TItem>
    where TItem : CommercialDocumentItem
{
    public override void Configure(EntityTypeBuilder<TItem> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.ProductId)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.ProductName)
            .HasColumnType("varchar(200)")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnType("numeric(18,3)")
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(x => x.VatRate)
            .HasColumnType("numeric(5,2)")
            .IsRequired();

        builder.Property(x => x.Subtotal)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(x => x.VatAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(x => x.Total)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}