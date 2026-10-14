using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Identity;

namespace Tezgah.Infrastructure.Persistence.Configurations.Identity;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> b)
    {
        b.Property(x => x.Title).HasMaxLength(50).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Property(x => x.City).HasMaxLength(60).IsRequired();
        b.Property(x => x.District).HasMaxLength(60).IsRequired();
        b.Property(x => x.Line).HasMaxLength(500).IsRequired();
        b.Property(x => x.PostalCode).HasMaxLength(10);
        b.HasIndex(x => x.UserId);
    }
}
