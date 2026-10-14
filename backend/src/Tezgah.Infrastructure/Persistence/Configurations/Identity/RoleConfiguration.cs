using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Identity;

namespace Tezgah.Infrastructure.Persistence.Configurations.Identity;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.Property(x => x.Name).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
        b.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
    }
}
