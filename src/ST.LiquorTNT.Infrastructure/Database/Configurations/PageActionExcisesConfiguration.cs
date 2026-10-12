using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class PageActionExcisesConfiguration : IEntityTypeConfiguration<PAGE_ACTION_EXCISES>
{
    public void Configure(EntityTypeBuilder<PAGE_ACTION_EXCISES> builder)
    {
        builder.ToTable("PAGE_ACTION_EXCISES");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(r => r.PageActionId).HasColumnName("PAGE_ACTION_ID");
        builder.Property(r => r.ExciseId).HasColumnName("EXCISE_ID");

        builder.HasIndex(r => new { r.PageActionId, r.ExciseId }).IsUnique().HasDatabaseName("UQ_PAGE_ACTION_EXCISE");
    }
}
