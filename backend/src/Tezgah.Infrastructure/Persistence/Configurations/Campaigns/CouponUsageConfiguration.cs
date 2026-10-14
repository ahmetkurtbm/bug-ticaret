using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Campaigns;

namespace Tezgah.Infrastructure.Persistence.Configurations.Campaigns;

public class CouponUsageConfiguration : IEntityTypeConfiguration<CouponUsage>
{
    public void Configure(EntityTypeBuilder<CouponUsage> b)
    {
        b.HasIndex(x => new { x.CouponId, x.UserId });
        b.HasIndex(x => x.OrderId).IsUnique();
        b.HasOne<Coupon>().WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Restrict);
    }
}
