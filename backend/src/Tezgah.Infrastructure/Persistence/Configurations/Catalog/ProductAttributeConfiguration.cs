using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Catalog;

namespace Tezgah.Infrastructure.Persistence.Configurations.Catalog;

public class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Unit).HasMaxLength(20);
        b.HasIndex(x => x.CategoryId);
        b.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
