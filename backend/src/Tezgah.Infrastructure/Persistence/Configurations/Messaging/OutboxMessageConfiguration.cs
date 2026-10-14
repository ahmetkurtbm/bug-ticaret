using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Messaging;

namespace Tezgah.Infrastructure.Persistence.Configurations.Messaging;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasMaxLength(60).IsRequired();
        b.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.LastError).HasMaxLength(1000);
        // Yayıncı yalnızca bekleyenleri tarar; kısmi index küçük kalır.
        b.HasIndex(x => x.NextAttemptAt).HasFilter("published_at IS NULL");
    }
}
