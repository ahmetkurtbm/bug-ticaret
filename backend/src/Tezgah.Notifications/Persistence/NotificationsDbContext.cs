using Microsoft.EntityFrameworkCore;
using Tezgah.Notifications.Domain;

namespace Tezgah.Notifications.Persistence;

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();
            e.Property(x => x.EventType).HasMaxLength(50);
            e.Property(x => x.RecipientEmail).HasMaxLength(256);
            e.Property(x => x.Subject).HasMaxLength(500);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.LastError).HasMaxLength(1000);
            // Tekilleştirme: aynı olay ikinci kez gelirse INSERT ... ON CONFLICT DO NOTHING 0 satır etkiler.
            e.HasIndex(x => x.EventId).IsUnique();
            e.HasIndex(x => x.RecipientEmail);
            e.HasIndex(x => new { x.Status, x.Attempts });
        });
    }
}
