using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Catalog;

namespace Tezgah.Infrastructure.Persistence.Configurations.Catalog;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.Property(x => x.Name).HasMaxLength(250).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(280).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.Property(x => x.Rating).HasPrecision(3, 2);
        b.HasIndex(x => new { x.CategoryId, x.Status });
        b.HasIndex(x => x.SellerId);
        b.HasIndex(x => x.BrandId);
        b.HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Variants).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Images).WithOne().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Attributes).WithOne().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
