using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Catalog;

namespace Tezgah.Infrastructure.Persistence.Configurations.Catalog;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> b)
    {
        b.Property(x => x.Url).HasMaxLength(500).IsRequired();
        b.Property(x => x.AltText).HasMaxLength(200);
        b.HasIndex(x => new { x.ProductId, x.SortOrder });
    }
}
