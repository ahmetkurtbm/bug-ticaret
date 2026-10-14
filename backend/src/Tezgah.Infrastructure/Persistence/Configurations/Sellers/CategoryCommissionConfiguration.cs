using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Sellers;
using Tezgah.Domain.Catalog;

namespace Tezgah.Infrastructure.Persistence.Configurations.Sellers;

public class CategoryCommissionConfiguration : IEntityTypeConfiguration<CategoryCommission>
{
    public void Configure(EntityTypeBuilder<CategoryCommission> b)
    {
        b.Property(x => x.Rate).HasPrecision(5, 2);
        b.HasIndex(x => new { x.SellerId, x.CategoryId, x.ValidFrom });
        b.HasOne<Seller>().WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
