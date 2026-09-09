using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderHub.Domain.Models;

namespace OrderHub.Infrastructure.Configurations;

internal sealed class UserPermissionConfiguration
    : ModelConfigurationBase<UserPermission>
{
    public override void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        base.Configure(builder);

        builder.ToTable("user_permissions");

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Feature)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CanRead)
            .IsRequired();

        builder.Property(x => x.CanWrite)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new
        {
            x.UserId,
            x.Feature
        })
        .IsUnique();
    }
}