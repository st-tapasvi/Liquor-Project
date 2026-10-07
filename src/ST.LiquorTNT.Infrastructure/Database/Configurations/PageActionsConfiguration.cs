using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class PageActionsConfiguration : IEntityTypeConfiguration<PAGE_ACTIONS>
{
    public void Configure(EntityTypeBuilder<PAGE_ACTIONS> builder)
    {
        builder.ToTable("PAGE_ACTIONS");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("ID");
        builder.Property(a => a.PageId).HasColumnName("PAGE_ID");
        builder.Property(a => a.ActionKey).HasColumnName("ACTION_KEY").HasMaxLength(30).IsRequired();
        builder.Property(a => a.PermissionKey).HasColumnName("PERMISSION_KEY").HasMaxLength(80).IsRequired();
        builder.Property(a => a.ActionName).HasColumnName("ACTION_NAME").HasMaxLength(100).IsRequired();
        builder.Property(a => a.GrantScope).HasColumnName("GRANT_SCOPE").HasMaxLength(10).HasConversion<string>();   // stored as ANY / ADMIN / SYSTEM
        builder.Property(a => a.SortOrder).HasColumnName("SORT_ORDER");
        builder.Property(a => a.IsActive).HasColumnName("IS_ACTIVE");

        builder.HasIndex(a => a.PermissionKey).IsUnique().HasDatabaseName("UQ_PAGE_ACTIONS_KEY");
    }
}
