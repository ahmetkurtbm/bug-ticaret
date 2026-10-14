using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Orders;

namespace Tezgah.Infrastructure.Persistence.Configurations.Orders;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.Property(x => x.OrderNumber).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.OrderNumber).IsUnique();
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.CouponCode).HasMaxLength(40);
        b.Property(x => x.IdempotencyKey).HasMaxLength(64);
        b.HasIndex(x => new { x.UserId, x.IdempotencyKey }).IsUnique().HasFilter("idempotency_key IS NOT NULL");
        b.HasIndex(x => new { x.Status, x.CreatedAt }).HasFilter("status = 1");
        b.HasIndex(x => new { x.UserId, x.PlacedAt });
        b.HasIndex(x => new { x.Status, x.PlacedAt });
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.OwnsOne(x => x.ShippingAddress, a => ConfigureAddress(a, "shipping"));
        b.OwnsOne(x => x.BillingAddress, a => ConfigureAddress(a, "billing"));
        b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.StatusHistory).WithOne().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureAddress<T>(OwnedNavigationBuilder<T, OrderAddress> a, string prefix) where T : class
    {
        a.Property(x => x.FullName).HasColumnName($"{prefix}_full_name").HasMaxLength(200);
        a.Property(x => x.Phone).HasColumnName($"{prefix}_phone").HasMaxLength(20);
        a.Property(x => x.City).HasColumnName($"{prefix}_city").HasMaxLength(60);
        a.Property(x => x.District).HasColumnName($"{prefix}_district").HasMaxLength(60);
        a.Property(x => x.Line).HasColumnName($"{prefix}_line").HasMaxLength(500);
        a.Property(x => x.PostalCode).HasColumnName($"{prefix}_postal_code").HasMaxLength(10);
    }
}
