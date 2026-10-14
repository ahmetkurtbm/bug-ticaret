using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Campaigns;

namespace Tezgah.Infrastructure.Persistence.Configurations.Campaigns;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> b)
    {
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.Description).HasMaxLength(250).IsRequired();
        b.HasIndex(x => new { x.IsActive, x.StartsAt, x.EndsAt });
    }
}
