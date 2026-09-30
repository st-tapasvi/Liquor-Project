using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class PasswordPolicyConfiguration : IEntityTypeConfiguration<PASSWORD_POLICY>
{
    public void Configure(EntityTypeBuilder<PASSWORD_POLICY> builder)
    {
        builder.ToTable("PASSWORD_POLICY");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(p => p.PolicyName).HasColumnName("POLICY_NAME").HasMaxLength(50).IsRequired();
        builder.Property(p => p.MinLength).HasColumnName("MIN_LENGTH");
        builder.Property(p => p.MaxLength).HasColumnName("MAX_LENGTH");
        builder.Property(p => p.RequireUppercase).HasColumnName("REQUIRE_UPPERCASE");
        builder.Property(p => p.RequireLowercase).HasColumnName("REQUIRE_LOWERCASE");
        builder.Property(p => p.RequireNumber).HasColumnName("REQUIRE_NUMBER");
        builder.Property(p => p.RequireSpecialCharacter).HasColumnName("REQUIRE_SPECIAL_CHARACTER");
        builder.Property(p => p.PasswordHistoryCount).HasColumnName("PASSWORD_HISTORY_COUNT");
        builder.Property(p => p.PasswordExpiryEnabled).HasColumnName("PASSWORD_EXPIRY_ENABLED");
        builder.Property(p => p.PasswordExpiryDays).HasColumnName("PASSWORD_EXPIRY_DAYS");
        builder.Property(p => p.AllowUsernameInPassword).HasColumnName("ALLOW_USERNAME_IN_PASSWORD");
        builder.Property(p => p.AllowCommonPassword).HasColumnName("ALLOW_COMMON_PASSWORD");
        builder.Property(p => p.Status).HasColumnName("STATUS");
        builder.Property(p => p.CreatedBy).HasColumnName("CREATED_BY");
        builder.Property(p => p.CreatedAt).HasColumnName("CREATED_AT");
        builder.Property(p => p.UpdatedBy).HasColumnName("UPDATED_BY");
        builder.Property(p => p.UpdatedAt).HasColumnName("UPDATED_AT");

        builder.HasIndex(p => p.PolicyName).IsUnique().HasDatabaseName("UQ_PASSWORD_POLICY_NAME");
    }
}
