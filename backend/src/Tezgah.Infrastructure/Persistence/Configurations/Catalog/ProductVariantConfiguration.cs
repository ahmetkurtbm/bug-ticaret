using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Catalog;

namespace Tezgah.Infrastructure.Persistence.Configurations.Catalog;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> b)
    {
        b.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.Sku).IsUnique();
        b.Property(x => x.Barcode).HasMaxLength(32);
        b.Property(x => x.Color).HasMaxLength(50);
        b.Property(x => x.Size).HasMaxLength(30);
        b.Property(x => x.VatRate).HasPrecision(5, 2);
        b.Property(x => x.WeightKg).HasPrecision(8, 3);
        b.HasIndex(x => x.ProductId);
        b.ToTable(t => t.HasCheckConstraint("ck_product_variants_price_positive", "price > 0"));
    }
}
