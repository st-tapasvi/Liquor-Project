using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class RolePasswordPolicyConfiguration : IEntityTypeConfiguration<ROLE_PASSWORD_POLICY>
{
    public void Configure(EntityTypeBuilder<ROLE_PASSWORD_POLICY> builder)
    {
        builder.ToTable("ROLE_PASSWORD_POLICY");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(r => r.RoleId).HasColumnName("ROLE_ID");
        builder.Property(r => r.PasswordPolicyId).HasColumnName("PASSWORD_POLICY_ID");
        builder.Property(r => r.CreatedBy).HasColumnName("CREATED_BY");
        builder.Property(r => r.CreatedAt).HasColumnName("CREATED_AT");
        builder.Property(r => r.UpdatedBy).HasColumnName("UPDATED_BY");
        builder.Property(r => r.UpdatedAt).HasColumnName("UPDATED_AT");

        builder.HasIndex(r => r.RoleId).IsUnique().HasDatabaseName("UQ_ROLE_PASSWORD_POLICY_ROLE");
    }
}
