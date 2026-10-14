using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Reviews;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Identity;

namespace Tezgah.Infrastructure.Persistence.Configurations.Reviews;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.Property(x => x.Comment).HasMaxLength(2000);
        b.HasIndex(x => new { x.ProductId, x.IsApproved, x.CreatedAt });
        b.HasIndex(x => x.UserId);
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable(t => t.HasCheckConstraint("ck_reviews_rating_range", "rating BETWEEN 1 AND 5"));
    }
}
