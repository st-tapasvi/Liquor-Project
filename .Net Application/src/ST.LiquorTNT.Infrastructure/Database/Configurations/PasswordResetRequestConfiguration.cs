using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class PasswordResetRequestConfiguration : IEntityTypeConfiguration<PASSWORD_RESET_REQUEST>
{
    public void Configure(EntityTypeBuilder<PASSWORD_RESET_REQUEST> builder)
    {
        builder.ToTable("PASSWORD_RESET_REQUEST");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(r => r.UserId).HasColumnName("USER_ID");
        builder.Property(r => r.RequestTokenHash).HasColumnName("REQUEST_TOKEN_HASH").HasMaxLength(255).IsRequired();
        builder.Property(r => r.Status).HasColumnName("STATUS").HasMaxLength(15).IsRequired();
        builder.Property(r => r.VerifyAttempts).HasColumnName("VERIFY_ATTEMPTS");
        builder.Property(r => r.ExpiresAt).HasColumnName("EXPIRES_AT");
        builder.Property(r => r.CreatedAt).HasColumnName("CREATED_AT");
        builder.Property(r => r.UpdatedAt).HasColumnName("UPDATED_AT");

        builder.HasIndex(r => r.RequestTokenHash).IsUnique().HasDatabaseName("UQ_PWRESET_TOKEN");
        builder.HasIndex(r => new { r.UserId, r.Status }).HasDatabaseName("IDX_PWRESET_USER");
    }
}
