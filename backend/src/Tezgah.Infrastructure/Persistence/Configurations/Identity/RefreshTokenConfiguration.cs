using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Identity;

namespace Tezgah.Infrastructure.Persistence.Configurations.Identity;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
        b.HasIndex(x => x.UserId);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
