using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Inventory;
using Tezgah.Domain.Catalog;

namespace Tezgah.Infrastructure.Persistence.Configurations.Inventory;

public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> b)
    {
        b.HasIndex(x => new { x.VariantId, x.WarehouseId }).IsUnique();
        b.Ignore(x => x.Available);
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_stock_items_quantity_non_negative", "quantity >= 0");
            t.HasCheckConstraint("ck_stock_items_reserved_valid", "reserved >= 0 AND reserved <= quantity");
        });
    }
}
