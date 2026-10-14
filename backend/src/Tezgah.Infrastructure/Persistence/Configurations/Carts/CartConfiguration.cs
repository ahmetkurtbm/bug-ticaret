using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Carts;
using Tezgah.Domain.Identity;

namespace Tezgah.Infrastructure.Persistence.Configurations.Carts;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> b)
    {
        b.Property(x => x.CouponCode).HasMaxLength(40);
        b.HasIndex(x => x.UserId).IsUnique().HasFilter("user_id IS NOT NULL");
        b.HasIndex(x => x.AnonymousId).IsUnique().HasFilter("anonymous_id IS NOT NULL");
        b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
