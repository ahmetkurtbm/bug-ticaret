using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Shipping;

namespace Tezgah.Infrastructure.Persistence.Configurations.Shipping;

public class ShipmentEventConfiguration : IEntityTypeConfiguration<ShipmentEvent>
{
    public void Configure(EntityTypeBuilder<ShipmentEvent> b)
    {
        b.Property(x => x.Location).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(250);
        b.HasIndex(x => new { x.ShipmentId, x.OccurredAt });
    }
}
