using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Sellers;
using Tezgah.Domain.Identity;

namespace Tezgah.Infrastructure.Persistence.Configurations.Sellers;

public class SellerConfiguration : IEntityTypeConfiguration<Seller>
{
    public void Configure(EntityTypeBuilder<Seller> b)
    {
        b.Property(x => x.StoreName).HasMaxLength(120).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(140).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.Property(x => x.LegalName).HasMaxLength(200).IsRequired();
        b.Property(x => x.TaxNumber).HasMaxLength(11).IsRequired();
        b.HasIndex(x => x.TaxNumber).IsUnique();
        b.Property(x => x.TaxOffice).HasMaxLength(100).IsRequired();
        b.Property(x => x.Iban).HasMaxLength(34);
        b.Property(x => x.City).HasMaxLength(60).IsRequired();
        b.Property(x => x.CommissionRate).HasPrecision(5, 2);
        b.Property(x => x.Rating).HasPrecision(3, 2);
        b.HasIndex(x => x.OwnerUserId).IsUnique(); // bir kullanıcının tek mağazası olur
        b.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
