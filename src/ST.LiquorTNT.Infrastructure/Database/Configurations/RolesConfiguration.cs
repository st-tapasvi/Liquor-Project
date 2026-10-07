using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class RolesConfiguration : IEntityTypeConfiguration<ROLES>
{
    public void Configure(EntityTypeBuilder<ROLES> builder)
    {
        builder.ToTable("ROLES");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(r => r.CompanyId).HasColumnName("COMPANY_ID");
        builder.Property(r => r.RoleName).HasColumnName("ROLE_NAME").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Description).HasColumnName("DESCRIPTION").HasMaxLength(255);
        builder.Property(r => r.IsSystem).HasColumnName("IS_SYSTEM");
        builder.Property(r => r.IsTemplate).HasColumnName("IS_TEMPLATE");
        builder.Property(r => r.IsAdminRole).HasColumnName("IS_ADMIN_ROLE");
        builder.Property(r => r.IsActive).HasColumnName("IS_ACTIVE");
        builder.Property(r => r.CreatedBy).HasColumnName("CREATED_BY");
        builder.Property(r => r.CreatedAt).HasColumnName("CREATED_AT");
        builder.Property(r => r.UpdatedBy).HasColumnName("UPDATED_BY");
        builder.Property(r => r.UpdatedAt).HasColumnName("UPDATED_AT");

        builder.HasIndex(r => new { r.CompanyId, r.RoleName }).IsUnique().HasDatabaseName("UQ_ROLES_COMPANY_NAME");
    }
}
