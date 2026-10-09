using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class RoleGroupRolesConfiguration : IEntityTypeConfiguration<ROLE_GROUP_ROLES>
{
    public void Configure(EntityTypeBuilder<ROLE_GROUP_ROLES> builder)
    {
        builder.ToTable("ROLE_GROUP_ROLES");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(r => r.RoleGroupId).HasColumnName("ROLE_GROUP_ID");
        builder.Property(r => r.RoleId).HasColumnName("ROLE_ID");
        builder.Property(r => r.CreatedBy).HasColumnName("CREATED_BY");
        builder.Property(r => r.CreatedAt).HasColumnName("CREATED_AT");

        builder.HasIndex(r => new { r.RoleGroupId, r.RoleId }).IsUnique().HasDatabaseName("UQ_ROLE_GROUP_ROLE");

        // The rows depend on their group (FK cascades in the database too), so EF deletes them before the group.
        builder.HasOne<ROLE_GROUP>().WithMany().HasForeignKey(r => r.RoleGroupId).OnDelete(DeleteBehavior.Cascade);
    }
}
