using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class RoleRightsConfiguration : IEntityTypeConfiguration<ROLE_RIGHTS>
{
    public void Configure(EntityTypeBuilder<ROLE_RIGHTS> builder)
    {
        builder.ToTable("ROLE_RIGHTS");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(r => r.RoleId).HasColumnName("ROLE_ID");
        builder.Property(r => r.PageActionId).HasColumnName("PAGE_ACTION_ID");
        builder.Property(r => r.CreatedBy).HasColumnName("CREATED_BY");
        builder.Property(r => r.CreatedAt).HasColumnName("CREATED_AT");

        builder.HasIndex(r => new { r.RoleId, r.PageActionId }).IsUnique().HasDatabaseName("UQ_ROLE_RIGHTS");

        // The rights depend on the role (FK_ROLE_RIGHTS_ROLE cascades in the database too), so EF deletes them first.
        builder.HasOne<ROLES>().WithMany().HasForeignKey(r => r.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}
