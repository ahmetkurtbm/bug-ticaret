using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Inventory;

namespace Tezgah.Infrastructure.Persistence.Configurations.Inventory;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.Property(x => x.ReferenceType).HasMaxLength(40);
        b.HasIndex(x => new { x.StockItemId, x.CreatedAt });
        b.HasOne<StockItem>().WithMany().HasForeignKey(x => x.StockItemId).OnDelete(DeleteBehavior.Cascade);
    }
}
