using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class LiquorCategoryConfiguration : IEntityTypeConfiguration<LIQUOR_CATEGORY>
{
    public void Configure(EntityTypeBuilder<LIQUOR_CATEGORY> builder)
    {
        builder.ToTable("LIQUOR_CATEGORY");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(c => c.CategoryCode).HasColumnName("CATEGORY_CODE").HasMaxLength(20).IsRequired();
        builder.Property(c => c.CategoryName).HasColumnName("CATEGORY_NAME").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasColumnName("DESCRIPTION").HasMaxLength(500);
        builder.Property(c => c.IsActive).HasColumnName("IS_ACTIVE");
        builder.Property(c => c.CreatedBy).HasColumnName("CREATED_BY");
        builder.Property(c => c.CreatedAt).HasColumnName("CREATED_AT");
        builder.Property(c => c.UpdatedBy).HasColumnName("UPDATED_BY");
        builder.Property(c => c.UpdatedAt).HasColumnName("UPDATED_AT");

        builder.HasIndex(c => c.CategoryCode).IsUnique().HasDatabaseName("UQ_LIQUOR_CATEGORY_CODE");
    }
}
