using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class SupplierCodeConfiguration : IEntityTypeConfiguration<SUPPLIER_CODE>
{
    public void Configure(EntityTypeBuilder<SUPPLIER_CODE> builder)
    {
        builder.ToTable("SUPPLIER_CODE");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(s => s.CompanyId).HasColumnName("COMPANY_ID");
        builder.Property(s => s.FranchiseName).HasColumnName("FRANCHISE_NAME").HasMaxLength(200);
        builder.Property(s => s.ExciseId).HasColumnName("EXCISE_ID");
        builder.Property(s => s.SupplierCode).HasColumnName("SUPPLIER_CODE").HasMaxLength(20).IsRequired();
        builder.Property(s => s.LiquorCategoryId).HasColumnName("LIQUOR_CATEGORY_ID");
        builder.Property(s => s.IsActive).HasColumnName("IS_ACTIVE");
        builder.Property(s => s.CreatedBy).HasColumnName("CREATED_BY");
        builder.Property(s => s.CreatedAt).HasColumnName("CREATED_AT");
        builder.Property(s => s.UpdatedBy).HasColumnName("UPDATED_BY");
        builder.Property(s => s.UpdatedAt).HasColumnName("UPDATED_AT");

        builder.HasIndex(s => new { s.ExciseId, s.SupplierCode }).IsUnique().HasDatabaseName("UQ_SUPPLIER_CODE_EXCISE");
    }
}
