using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Tezgah.Domain.Audit;
using Tezgah.Domain.Campaigns;
using Tezgah.Domain.Carts;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Identity;
using Tezgah.Domain.Inventory;
using Tezgah.Domain.Orders;
using Tezgah.Domain.Payments;
using Tezgah.Domain.Reviews;
using Tezgah.Domain.Sellers;
using Tezgah.Domain.Shipping;

namespace Tezgah.Application.Common.Interfaces;

public interface ITezgahDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Address> Addresses { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Seller> Sellers { get; }
    DbSet<CategoryCommission> CategoryCommissions { get; }

    DbSet<Category> Categories { get; }
    DbSet<Brand> Brands { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<ProductAttribute> ProductAttributes { get; }
    DbSet<ProductAttributeValue> ProductAttributeValues { get; }
    DbSet<PriceHistory> PriceHistory { get; }

    DbSet<Warehouse> Warehouses { get; }
    DbSet<StockItem> StockItems { get; }
    DbSet<StockMovement> StockMovements { get; }

    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }

    DbSet<Coupon> Coupons { get; }
    DbSet<CouponUsage> CouponUsages { get; }

    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<OrderStatusHistory> OrderStatusHistory { get; }

    DbSet<Payment> Payments { get; }
    DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents { get; }
    DbSet<Shipment> Shipments { get; }
    DbSet<ShipmentEvent> ShipmentEvents { get; }
    DbSet<Review> Reviews { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Tezgah.Domain.Messaging.OutboxMessage> OutboxMessages { get; }

    /// <summary>İşlem (transaction) ve toplu güncelleme için.</summary>
    DatabaseFacade Database { get; }

    /// <summary>Başarısız bir işlemden sonra izlenen varlıkları temizlemek için.</summary>
    Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker ChangeTracker { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
