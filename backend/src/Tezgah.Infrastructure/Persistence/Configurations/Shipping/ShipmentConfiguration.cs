using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Shipping;
using Tezgah.Domain.Orders;

namespace Tezgah.Infrastructure.Persistence.Configurations.Shipping;

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> b)
    {
        b.Property(x => x.Carrier).HasMaxLength(40).IsRequired();
        b.Property(x => x.TrackingNumber).HasMaxLength(60);
        b.HasIndex(x => x.TrackingNumber);
        b.HasIndex(x => new { x.OrderId, x.SellerId });
        b.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
