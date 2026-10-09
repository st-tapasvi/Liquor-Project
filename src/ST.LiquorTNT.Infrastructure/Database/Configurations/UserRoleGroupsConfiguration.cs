using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class UserRoleGroupsConfiguration : IEntityTypeConfiguration<USER_ROLE_GROUPS>
{
    public void Configure(EntityTypeBuilder<USER_ROLE_GROUPS> builder)
    {
        builder.ToTable("USER_ROLE_GROUPS");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(r => r.UserId).HasColumnName("USER_ID");
        builder.Property(r => r.RoleGroupId).HasColumnName("ROLE_GROUP_ID");
        builder.Property(r => r.CreatedBy).HasColumnName("CREATED_BY");
        builder.Property(r => r.CreatedAt).HasColumnName("CREATED_AT");

        builder.HasIndex(r => new { r.UserId, r.RoleGroupId }).IsUnique().HasDatabaseName("UQ_USER_ROLE_GROUP");
    }
}
