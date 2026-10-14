using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Orders;

namespace Tezgah.Infrastructure.Persistence.Configurations.Orders;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.Property(x => x.ProductName).HasMaxLength(250).IsRequired();
        b.Property(x => x.Sku).HasMaxLength(64).IsRequired();
        b.Property(x => x.VariantLabel).HasMaxLength(100);
        b.Property(x => x.VatRate).HasPrecision(5, 2);
        b.Property(x => x.CommissionRate).HasPrecision(5, 2);
        b.HasIndex(x => x.OrderId);
        b.HasIndex(x => new { x.SellerId, x.OrderId });
        b.HasIndex(x => x.VariantId);
        b.HasOne<Tezgah.Domain.Inventory.StockItem>().WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.SetNull);
        b.ToTable(t => t.HasCheckConstraint("ck_order_items_quantity_positive", "quantity > 0"));
    }
}
