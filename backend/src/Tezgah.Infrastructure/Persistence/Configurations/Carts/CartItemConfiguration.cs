using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Carts;

namespace Tezgah.Infrastructure.Persistence.Configurations.Carts;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> b)
    {
        b.HasIndex(x => new { x.CartId, x.VariantId }).IsUnique();
        b.HasOne(x => x.Variant).WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Cascade);
        b.ToTable(t => t.HasCheckConstraint("ck_cart_items_quantity_positive", "quantity > 0"));
    }
}
