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
        builder.Property(r => r.Id).HasColumnName("ID");
        builder.Property(r => r.RoleName).HasColumnName("ROLE_NAME").HasMaxLength(100).IsRequired();
        builder.Property(r => r.IsActive).HasColumnName("IS_ACTIVE");
    }
}
