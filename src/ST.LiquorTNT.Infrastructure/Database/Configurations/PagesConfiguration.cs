using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class PagesConfiguration : IEntityTypeConfiguration<PAGES>
{
    public void Configure(EntityTypeBuilder<PAGES> builder)
    {
        builder.ToTable("PAGES");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("ID");
        builder.Property(p => p.PageName).HasColumnName("PAGE_NAME").HasMaxLength(100).IsRequired();
        builder.Property(p => p.ModuleName).HasColumnName("MODULE_NAME").HasMaxLength(100);
        builder.Property(p => p.PageKey).HasColumnName("PAGE_KEY").HasMaxLength(50);
        builder.Property(p => p.SortOrder).HasColumnName("SORT_ORDER");
        builder.Property(p => p.IsActive).HasColumnName("IS_ACTIVE");
    }
}
