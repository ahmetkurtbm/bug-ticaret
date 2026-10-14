using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Payments;
using Tezgah.Domain.Orders;

namespace Tezgah.Infrastructure.Persistence.Configurations.Payments;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.Property(x => x.Provider).HasMaxLength(40).IsRequired();
        b.Property(x => x.ProviderPaymentId).HasMaxLength(100);
        b.HasIndex(x => x.ProviderPaymentId).IsUnique().HasFilter("provider_payment_id IS NOT NULL");
        b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        b.Property(x => x.FailureReason).HasMaxLength(500);
        b.HasIndex(x => x.OrderId);
        b.HasOne<Order>().WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
