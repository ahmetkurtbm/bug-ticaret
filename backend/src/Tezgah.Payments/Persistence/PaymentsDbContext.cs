using Microsoft.EntityFrameworkCore;
using Tezgah.Payments.Domain;

namespace Tezgah.Payments.Persistence;

public class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("payments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.OrderNumber).HasMaxLength(20);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.CustomerEmail).HasMaxLength(256);
            e.Property(x => x.ReturnUrl).HasMaxLength(500);
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.FailureReason).HasMaxLength(500);
            // Aynı siparişin bekleyen ödemesini bulmak için; unique değil (başarısız denemeden sonra yenisi açılabilir).
            e.HasIndex(x => x.OrderId);
        });

        modelBuilder.Entity<WebhookDelivery>(e =>
        {
            e.ToTable("webhook_deliveries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.Payload).HasColumnType("jsonb");
            e.Property(x => x.LastError).HasMaxLength(1000);
            e.HasOne(x => x.Payment).WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.DeliveredAt, x.NextAttemptAt });
        });
    }
}
