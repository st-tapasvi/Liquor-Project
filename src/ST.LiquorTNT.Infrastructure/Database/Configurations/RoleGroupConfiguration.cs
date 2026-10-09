using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class RoleGroupConfiguration : IEntityTypeConfiguration<ROLE_GROUP>
{
    public void Configure(EntityTypeBuilder<ROLE_GROUP> builder)
    {
        builder.ToTable("ROLE_GROUP");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(g => g.CompanyId).HasColumnName("COMPANY_ID");
        builder.Property(g => g.GroupName).HasColumnName("GROUP_NAME").HasMaxLength(100).IsRequired();
        builder.Property(g => g.Description).HasColumnName("DESCRIPTION").HasMaxLength(255);
        builder.Property(g => g.IsActive).HasColumnName("IS_ACTIVE");
        builder.Property(g => g.CreatedBy).HasColumnName("CREATED_BY");
        builder.Property(g => g.CreatedAt).HasColumnName("CREATED_AT");
        builder.Property(g => g.UpdatedBy).HasColumnName("UPDATED_BY");
        builder.Property(g => g.UpdatedAt).HasColumnName("UPDATED_AT");

        builder.HasIndex(g => new { g.CompanyId, g.GroupName }).IsUnique().HasDatabaseName("UQ_ROLE_GROUP_COMPANY_NAME");
    }
}
