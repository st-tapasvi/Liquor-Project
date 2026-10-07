using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<COMPANY>
{
    public void Configure(EntityTypeBuilder<COMPANY> builder)
    {
        builder.ToTable("COMPANY");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("ID");
        builder.Property(c => c.CompanyName).HasColumnName("COMPANY_NAME").HasMaxLength(500);
        builder.Property(c => c.AliasName).HasColumnName("ALIAS_NAME").HasMaxLength(500);
        builder.Property(c => c.City).HasColumnName("CITY").HasMaxLength(50);
        builder.Property(c => c.IsActive).HasColumnName("IS_ACTIVE");
    }
}
