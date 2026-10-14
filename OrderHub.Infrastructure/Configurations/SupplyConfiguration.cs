using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class SupplyConfiguration : ModelConfigurationBase<Supply>
{
    public override void Configure(EntityTypeBuilder<Supply> builder)
    {
        base.Configure(builder);
        builder.ToTable("supplies", table =>
            table.HasCheckConstraint("ck_supplies_numbering", "work_record_number > 0 AND sequence_year > 0 AND sequence_month BETWEEN 1 AND 12"));
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.WorkRecordNumber).IsRequired();
        builder.Property(x => x.SequenceYear).IsRequired();
        builder.Property(x => x.SequenceMonth).IsRequired();
        builder.Ignore(x => x.StatusHistory);
        builder.Ignore("_statusHistory");
        builder.HasOne(x => x.Order).WithMany(x => x.Supplies).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.OrderId, x.SupplierId });
        builder.HasIndex(x => new { x.SupplierId, x.SequenceYear, x.SequenceMonth, x.WorkRecordNumber }).IsUnique();
    }
}
