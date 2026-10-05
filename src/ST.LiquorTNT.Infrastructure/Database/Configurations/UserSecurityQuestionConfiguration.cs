using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ST.LiquorTNT.Domain.Entities;

namespace ST.LiquorTNT.Infrastructure.Database.Configurations;

public sealed class UserSecurityQuestionConfiguration : IEntityTypeConfiguration<USER_SECURITY_QUESTION>
{
    public void Configure(EntityTypeBuilder<USER_SECURITY_QUESTION> builder)
    {
        builder.ToTable("USER_SECURITY_QUESTION");

        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).HasColumnName("ID").ValueGeneratedOnAdd();
        builder.Property(q => q.UserId).HasColumnName("USER_ID");
        builder.Property(q => q.QuestionId).HasColumnName("QUESTION_ID");
        builder.Property(q => q.AnswerHash).HasColumnName("ANSWER_HASH").HasMaxLength(255).IsRequired();
        builder.Property(q => q.IsActive).HasColumnName("IS_ACTIVE");
        builder.Property(q => q.CreatedAt).HasColumnName("CREATED_AT");
        builder.Property(q => q.UpdatedAt).HasColumnName("UPDATED_AT");

        builder.HasIndex(q => new { q.UserId, q.QuestionId }).IsUnique().HasDatabaseName("UQ_USER_QUESTION");
    }
}
