using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ST.LiquorTNT.Domain.Entities;
using ST.LiquorTNT.Infrastructure.Database;
using Xunit;

namespace ST.LiquorTNT.Infrastructure.Tests.Database;

/// <summary>
/// db/mysql/014: a security question someone has answered never changes its text (trigger) and cannot be deleted
/// (foreign key), so a stored answer always belongs to the question the user saw. An unused question may be corrected.
/// </summary>
public sealed class SecurityQuestionLockTests
{
    private static readonly string Connection =
        Environment.GetEnvironmentVariable("ST_TNT_TEST_CONNECTION")
        ?? "Server=192.168.1.99;Port=3306;Database=st_tnt_liquor;User ID=root;Password=root;";

    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(Connection, new MySqlServerVersion(new Version(8, 0, 46)))
            .Options);

    [Fact]
    public async Task QuestionInUse_TextCannotChange_NorBeDeleted_UnusedQuestionCanBeCorrected()
    {
        var tag = Guid.NewGuid().ToString("N")[..6];
        var text = $"ZZ test question {tag}?";
        await using var db = NewContext();

        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO SECURITY_QUESTION (QUESTION_TEXT, STATUS) VALUES ({text}, 1)");
        var questionId = await db.SECURITY_QUESTION.Where(q => q.QuestionText == text).Select(q => q.Id).SingleAsync();
        var user = USERS.Create("zz_sq_" + tag, "H:x", null, null, null, null, null, false, null, DateTime.Now, null);
        db.Add(user);
        await db.SaveChangesAsync();

        try
        {
            // unused: a typo may still be fixed
            var corrected = text + " ";
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE SECURITY_QUESTION SET QUESTION_TEXT = {corrected} WHERE ID = {questionId}");
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE SECURITY_QUESTION SET QUESTION_TEXT = {text} WHERE ID = {questionId}");

            db.Add(USER_SECURITY_QUESTION.Create(user.Id, questionId, "H:answer", DateTime.Now));
            await db.SaveChangesAsync();

            // in use: no text change, not even upper / lower case
            var changed = text.ToUpperInvariant();
            var rename = () => db.Database.ExecuteSqlInterpolatedAsync($"UPDATE SECURITY_QUESTION SET QUESTION_TEXT = {changed} WHERE ID = {questionId}");
            (await rename.Should().ThrowAsync<Exception>()).Which.Message.Should().Contain("in use");

            var delete = () => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM SECURITY_QUESTION WHERE ID = {questionId}");
            await delete.Should().ThrowAsync<Exception>();

            // retiring it is still allowed
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE SECURITY_QUESTION SET STATUS = 0 WHERE ID = {questionId}");
            (await db.SECURITY_QUESTION.AsNoTracking().SingleAsync(q => q.Id == questionId)).Status.Should().BeFalse();
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM USERS WHERE ID = {user.Id}");        // user question cascades
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM SECURITY_QUESTION WHERE ID = {questionId}");
        }
    }
}
