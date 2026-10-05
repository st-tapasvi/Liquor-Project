using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class SecurityQuestionConfiguration : IEntityTypeConfiguration<SECURITY_QUESTION>
{
    public void Configure(EntityTypeBuilder<SECURITY_QUESTION> builder)
    {
        builder.ToTable("SECURITY_QUESTION");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(q => q.QuestionText).HasColumnName("QUESTION_TEXT").HasMaxLength(200).IsRequired();
        builder.Property(q => q.Status).HasColumnName("STATUS");
        builder.Property(q => q.CreatedAt).HasColumnName("CREATED_AT");
        builder.Property(q => q.UpdatedAt).HasColumnName("UPDATED_AT");

        builder.HasIndex(q => q.QuestionText).IsUnique().HasDatabaseName("UQ_SECURITY_QUESTION_TEXT");
    }
}
