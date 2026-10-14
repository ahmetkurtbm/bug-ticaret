using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Audit;

namespace Tezgah.Infrastructure.Persistence.Configurations.Audit;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.Property(x => x.Action).HasMaxLength(80).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
        b.Property(x => x.Data).HasColumnType("jsonb");
        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.HasIndex(x => new { x.EntityType, x.EntityId });
        b.HasIndex(x => x.CreatedAt);
    }
}
