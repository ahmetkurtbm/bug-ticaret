using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tezgah.Domain.Catalog;

namespace Tezgah.Infrastructure.Persistence.Configurations.Catalog;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Slug).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.Slug).IsUnique();
        b.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}
