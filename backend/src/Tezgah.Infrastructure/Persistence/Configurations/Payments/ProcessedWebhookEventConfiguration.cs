using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Payments;

namespace Tezgah.Infrastructure.Persistence.Configurations.Payments;

public class ProcessedWebhookEventConfiguration : IEntityTypeConfiguration<ProcessedWebhookEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhookEvent> b)
    {
        b.HasKey(x => x.EventId);
        b.Property(x => x.Source).HasMaxLength(40).IsRequired();
        b.HasIndex(x => x.ProcessedAt);
    }
}
