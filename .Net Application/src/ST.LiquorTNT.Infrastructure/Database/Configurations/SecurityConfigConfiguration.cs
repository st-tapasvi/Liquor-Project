using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class SecurityConfigConfiguration : IEntityTypeConfiguration<SECURITY_CONFIG>
{
    public void Configure(EntityTypeBuilder<SECURITY_CONFIG> builder)
    {
        builder.ToTable("SECURITY_CONFIG");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(c => c.ConfigKey).HasColumnName("CONFIG_KEY").HasMaxLength(60).IsRequired();
        builder.Property(c => c.ConfigValue).HasColumnName("CONFIG_VALUE").HasMaxLength(200).IsRequired();
        builder.Property(c => c.DataType).HasColumnName("DATA_TYPE").HasMaxLength(10).IsRequired();
        builder.Property(c => c.Description).HasColumnName("DESCRIPTION").HasMaxLength(255);
        builder.Property(c => c.IsActive).HasColumnName("IS_ACTIVE");
        builder.Property(c => c.UpdatedBy).HasColumnName("UPDATED_BY");
        builder.Property(c => c.UpdatedAt).HasColumnName("UPDATED_AT");

        builder.HasIndex(c => c.ConfigKey).IsUnique().HasDatabaseName("UQ_SECURITY_CONFIG_KEY");
    }
}
