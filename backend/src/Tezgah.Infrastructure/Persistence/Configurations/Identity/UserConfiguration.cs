using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Identity;

namespace Tezgah.Infrastructure.Persistence.Configurations.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.NormalizedEmail).HasMaxLength(256).IsRequired();
        b.HasIndex(x => x.NormalizedEmail).IsUnique();
        b.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Ignore(x => x.FullName);
        b.HasMany(x => x.Addresses).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
