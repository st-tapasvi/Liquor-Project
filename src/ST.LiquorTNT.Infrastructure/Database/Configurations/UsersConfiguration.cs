using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class UsersConfiguration : IEntityTypeConfiguration<USERS>
{
    public void Configure(EntityTypeBuilder<USERS> builder)
    {
        builder.ToTable("USERS");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("ID").ValueGeneratedOnAdd();

        // User names are case-INsensitive by decision ("Bob" and "bob" are the same account), set
        // explicitly rather than inherited from the table default (standards §8.3).
        builder.Property(u => u.UserName).HasColumnName("USERNAME").HasMaxLength(50).IsRequired()
               .UseCollation("utf8mb4_0900_ai_ci");
        builder.Property(u => u.FullName).HasColumnName("FULL_NAME").HasMaxLength(100);
        builder.Property(u => u.Email).HasColumnName("EMAIL").HasMaxLength(100);
        builder.Property(u => u.Phone).HasColumnName("PHONE").HasMaxLength(20);
        builder.Property(u => u.EmployeeCode).HasColumnName("EMPLOYEE_CODE").HasMaxLength(50);

        builder.Property(u => u.RoleId).HasColumnName("ROLE_ID");
        builder.Property(u => u.CompanyId).HasColumnName("COMPANY_ID");

        builder.Property(u => u.PasswordHash).HasColumnName("PASSWORD_HASH").HasMaxLength(255);
        builder.Property(u => u.PasswordChangedAt).HasColumnName("PASSWORD_CHANGED_AT");
        builder.Property(u => u.PasswordExpiresAt).HasColumnName("PASSWORD_EXPIRES_AT");
        builder.Property(u => u.ForcePasswordChange).HasColumnName("FORCE_PASSWORD_CHANGE");

        builder.Property(u => u.FailedLoginAttempts).HasColumnName("FAILED_LOGIN_ATTEMPTS");
        builder.Property(u => u.LastFailedLoginAt).HasColumnName("LAST_FAILED_LOGIN_AT");
        builder.Property(u => u.IsBlocked).HasColumnName("IS_BLOCKED");
        builder.Property(u => u.LockedUntil).HasColumnName("LOCKED_UNTIL");

        builder.Property(u => u.LastLoginAt).HasColumnName("LAST_LOGIN_AT");
        builder.Property(u => u.LastLoginIp).HasColumnName("LAST_LOGIN_IP").HasMaxLength(45);

        builder.Property(u => u.IsActive).HasColumnName("IS_ACTIVE");

        builder.Property(u => u.CreatedBy).HasColumnName("CREATED_BY");
        builder.Property(u => u.CreatedAt).HasColumnName("CREATED_AT");
        builder.Property(u => u.UpdatedBy).HasColumnName("UPDATED_BY");
        builder.Property(u => u.UpdatedAt).HasColumnName("UPDATED_AT");

        builder.HasIndex(u => u.UserName).IsUnique().HasDatabaseName("UNIQ_USERNAME");

        // Password history is written through the entity (USERS.SetPassword) and saved with it.
        builder.HasMany(u => u.PasswordHistory)
               .WithOne()
               .HasForeignKey(h => h.UserId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.PasswordHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
