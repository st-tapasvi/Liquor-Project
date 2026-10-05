using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class UserPasswordHistoryConfiguration : IEntityTypeConfiguration<USER_PASSWORD_HISTORY>
{
    public void Configure(EntityTypeBuilder<USER_PASSWORD_HISTORY> builder)
    {
        builder.ToTable("USER_PASSWORD_HISTORY");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(h => h.UserId).HasColumnName("USER_ID");
        builder.Property(h => h.PasswordHash).HasColumnName("PASSWORD_HASH").HasMaxLength(255).IsRequired();
        builder.Property(h => h.CreatedAt).HasColumnName("CREATED_AT");

        builder.HasIndex(h => new { h.UserId, h.CreatedAt }).HasDatabaseName("IDX_PWHIST_USER_CREATED");
    }
}
