using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class UserRightsConfiguration : IEntityTypeConfiguration<USER_RIGHTS>
{
    public void Configure(EntityTypeBuilder<USER_RIGHTS> builder)
    {
        builder.ToTable("USER_RIGHTS");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(r => r.UserId).HasColumnName("USER_ID");
        builder.Property(r => r.PageActionId).HasColumnName("PAGE_ACTION_ID");
        builder.Property(r => r.SupplierCodeId).HasColumnName("SUPPLIER_CODE_ID");
        builder.Property(r => r.CreatedBy).HasColumnName("CREATED_BY");
        builder.Property(r => r.CreatedAt).HasColumnName("CREATED_AT");

        builder.HasIndex(r => new { r.UserId, r.PageActionId, r.SupplierCodeId }).IsUnique().HasDatabaseName("UQ_USER_RIGHTS");
    }
}
