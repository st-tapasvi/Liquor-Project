using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class UserLogConfiguration : IEntityTypeConfiguration<USER_LOG>
{
    public void Configure(EntityTypeBuilder<USER_LOG> builder)
    {
        builder.ToTable("USER_LOG");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(l => l.UserId).HasColumnName("USER_ID");
        builder.Property(l => l.ActionType).HasColumnName("ACTION_TYPE").HasMaxLength(100).IsRequired();
        builder.Property(l => l.ModuleName).HasColumnName("MODULE_NAME").HasMaxLength(100);
        builder.Property(l => l.EntityName).HasColumnName("ENTITY_NAME").HasMaxLength(100);
        builder.Property(l => l.EntityId).HasColumnName("ENTITY_ID").HasMaxLength(50);
        builder.Property(l => l.ActionStatus).HasColumnName("ACTION_STATUS").HasMaxLength(20);
        builder.Property(l => l.Description).HasColumnName("DESCRIPTION").HasMaxLength(500);
        builder.Property(l => l.DateTime).HasColumnName("DATE_TIME");
        builder.Property(l => l.IpAddress).HasColumnName("IP_ADDRESS").HasMaxLength(45);
        builder.Property(l => l.UserAgent).HasColumnName("USER_AGENT").HasMaxLength(255);
        builder.Property(l => l.CorrelationId).HasColumnName("CORRELATION_ID").HasMaxLength(64);
        builder.Property(l => l.OldValue).HasColumnName("OLD_VALUE").HasColumnType("json");
        builder.Property(l => l.NewValue).HasColumnName("NEW_VALUE").HasColumnType("json");

        // PAGE_ID, EXCISE_CODE, ALLOTED_PLANT_ID and DEVICE_INFO exist on the table (legacy) but are
        // nullable and not used by this module, so they are intentionally left unmapped.
    }
}
