using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Orders;

namespace Tezgah.Infrastructure.Persistence.Configurations.Orders;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> b)
    {
        b.ToTable("order_status_history");
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasIndex(x => new { x.OrderId, x.ChangedAt });
    }
}
