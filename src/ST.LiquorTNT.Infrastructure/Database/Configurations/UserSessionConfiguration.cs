using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class UserSessionConfiguration : IEntityTypeConfiguration<USER_SESSION>
{
    public void Configure(EntityTypeBuilder<USER_SESSION> builder)
    {
        builder.ToTable("USER_SESSION");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(s => s.UserId).HasColumnName("USER_ID");
        builder.Property(s => s.ActiveSupplierCodeId).HasColumnName("ACTIVE_SUPPLIER_CODE_ID");
        builder.Property(s => s.SessionTokenHash).HasColumnName("SESSION_TOKEN_HASH").HasMaxLength(255).IsRequired();
        builder.Property(s => s.LoginAt).HasColumnName("LOGIN_AT");
        builder.Property(s => s.LastActivityAt).HasColumnName("LAST_ACTIVITY_AT");
        builder.Property(s => s.ExpiresAt).HasColumnName("EXPIRES_AT");
        builder.Property(s => s.AbsoluteExpiresAt).HasColumnName("ABSOLUTE_EXPIRES_AT");
        builder.Property(s => s.LogoutAt).HasColumnName("LOGOUT_AT");
        builder.Property(s => s.IpAddress).HasColumnName("IP_ADDRESS").HasMaxLength(45);
        builder.Property(s => s.UserAgent).HasColumnName("USER_AGENT").HasMaxLength(255);
        builder.Property(s => s.DeviceInfo).HasColumnName("DEVICE_INFO").HasMaxLength(255);
        builder.Property(s => s.Status).HasColumnName("STATUS").HasMaxLength(15).IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("CREATED_AT");

        builder.HasIndex(s => s.SessionTokenHash).IsUnique().HasDatabaseName("UQ_SESSION_TOKEN");
        builder.HasIndex(s => new { s.UserId, s.Status }).HasDatabaseName("IDX_SESSION_USER_STATUS");
        builder.HasIndex(s => s.ExpiresAt).HasDatabaseName("IDX_SESSION_EXPIRES");
    }
}
