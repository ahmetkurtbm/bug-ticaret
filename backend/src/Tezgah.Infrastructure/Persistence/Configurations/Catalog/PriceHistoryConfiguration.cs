using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Catalog;

namespace Tezgah.Infrastructure.Persistence.Configurations.Catalog;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> b)
    {
        b.ToTable("price_history");
        b.HasIndex(x => new { x.VariantId, x.ChangedAt });
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Cascade);
    }
}
