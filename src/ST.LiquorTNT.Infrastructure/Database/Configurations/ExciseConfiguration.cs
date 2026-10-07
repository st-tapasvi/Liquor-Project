using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class ExciseConfiguration : IEntityTypeConfiguration<EXCISE>
{
    public void Configure(EntityTypeBuilder<EXCISE> builder)
    {
        builder.ToTable("EXCISE");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("EXCISE_ID");
        builder.Property(e => e.ExciseCode).HasColumnName("EXCISE_CODE").HasMaxLength(5).IsRequired();
        builder.Property(e => e.ExciseName).HasColumnName("EXCISE_NAME").HasMaxLength(100).IsRequired();
        builder.Property(e => e.IsActive).HasColumnName("IS_ACTIVE");
    }
}
