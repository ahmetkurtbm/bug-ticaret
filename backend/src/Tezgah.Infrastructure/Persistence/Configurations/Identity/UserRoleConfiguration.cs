using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Identity;

namespace Tezgah.Infrastructure.Persistence.Configurations.Identity;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.HasKey(x => new { x.UserId, x.RoleId });
        b.HasOne(x => x.User).WithMany(x => x.Roles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
    }
}
