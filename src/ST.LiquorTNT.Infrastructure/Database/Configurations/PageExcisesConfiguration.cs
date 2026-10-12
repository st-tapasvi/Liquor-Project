using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class PageExcisesConfiguration : IEntityTypeConfiguration<PAGE_EXCISES>
{
    public void Configure(EntityTypeBuilder<PAGE_EXCISES> builder)
    {
        builder.ToTable("PAGE_EXCISES");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(r => r.PageId).HasColumnName("PAGE_ID");
        builder.Property(r => r.ExciseId).HasColumnName("EXCISE_ID");

        builder.HasIndex(r => new { r.PageId, r.ExciseId }).IsUnique().HasDatabaseName("UQ_PAGE_EXCISE");
    }
}
