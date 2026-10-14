using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Catalog;

namespace Tezgah.Infrastructure.Persistence.Configurations.Catalog;

public class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> b)
    {
        b.Property(x => x.Value).HasMaxLength(200).IsRequired();
        b.HasIndex(x => new { x.ProductId, x.AttributeId }).IsUnique();
        b.HasOne(x => x.Attribute).WithMany().HasForeignKey(x => x.AttributeId).OnDelete(DeleteBehavior.Cascade);
    }
}
