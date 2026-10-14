using Microsoft.EntityFrameworkCore;
using Tezgah.Application.Common.Interfaces;
using Tezgah.Domain.Audit;
using Tezgah.Domain.Campaigns;
using Tezgah.Domain.Carts;
using Tezgah.Domain.Catalog;
using Tezgah.Domain.Common;
using Tezgah.Domain.Identity;
using Tezgah.Domain.Inventory;
using Tezgah.Domain.Orders;
using Tezgah.Domain.Payments;
using Tezgah.Domain.Reviews;
using Tezgah.Domain.Sellers;
using Tezgah.Domain.Shipping;

namespace Tezgah.Infrastructure.Persistence;

public class TezgahDbContext(DbContextOptions<TezgahDbContext> options, TimeProvider clock)
    : DbContext(options), ITezgahDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<CategoryCommission> CategoryCommissions => Set<CategoryCommission>();

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<PriceHistory> PriceHistory => Set<PriceHistory>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistory => Set<OrderStatusHistory>();

    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents => Set<ProcessedWebhookEvent>();
    public DbSet<Tezgah.Domain.Messaging.OutboxMessage> OutboxMessages => Set<Tezgah.Domain.Messaging.OutboxMessage>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TezgahDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Para alanları varsayılan olarak 18,2. Oranlar yapılandırmada ayrıca 5,2 verilir.
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
                entry.Entity.CreatedAt = now;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
