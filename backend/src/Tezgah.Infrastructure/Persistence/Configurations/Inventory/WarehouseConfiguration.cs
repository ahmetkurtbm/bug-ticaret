using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Inventory;
using Tezgah.Domain.Sellers;

namespace Tezgah.Infrastructure.Persistence.Configurations.Inventory;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.City).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.SellerId);
        b.HasOne<Seller>().WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Cascade);
    }
}
