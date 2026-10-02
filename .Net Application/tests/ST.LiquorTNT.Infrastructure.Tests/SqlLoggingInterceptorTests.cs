using FluentAssertions;
using ST.LiquorTNT.Infrastructure.Database;
using Xunit;

namespace ST.LiquorTNT.Infrastructure.Tests;

/// <summary>The DETAIL-mode SQL must be runnable in MySQL: real values in, secrets masked.</summary>
public sealed class SqlLoggingInterceptorTests
{
    private static string Build(string sql, params (string, object?)[] parameters) =>
        SqlLoggingInterceptor.BuildRunnableSql(sql, parameters);

    [Fact]
    public void Values_ArePutBack_StringsQuoted_NumbersAndDatesTyped()
    {
        var sql = Build(
            "SELECT * FROM USERS WHERE USERNAME = @__userName_0 AND ID = @__id_1 AND CREATED_AT > @__from_2 AND IS_ACTIVE = @__active_3",
            ("__userName_0", "admin"), ("__id_1", 7), ("__from_2", new DateTime(2026, 9, 30, 12, 0, 0)), ("__active_3", true));

        sql.Should().Be(
            "SELECT * FROM USERS WHERE USERNAME = 'admin' AND ID = 7 AND CREATED_AT > '2026-09-30 12:00:00' AND IS_ACTIVE = 1");
    }

    [Fact]
    public void PasswordHashAndTokenHash_AreMasked_NothingElse()
    {
        var sql = Build(
            "INSERT INTO USERS (USERNAME, PASSWORD_HASH) VALUES (@p0, @p1)",
            ("p0", "ravi"), ("p1", "PBKDF2.SHA256.100000.abc==.def=="));
        sql.Should().Contain("'ravi'").And.Contain("'***'").And.NotContain("PBKDF2");

        var session = Build("INSERT INTO USER_SESSION (SESSION_TOKEN_HASH) VALUES (@p0)",
            ("p0", new string('a', 64)));                       // 64 hex chars = a SHA-256 token hash
        session.Should().Be("INSERT INTO USER_SESSION (SESSION_TOKEN_HASH) VALUES ('***')");
    }

    [Fact]
    public void LongerNamesFirst_SoAtP1DoesNotBreakAtP10()
    {
        var sql = Build("VALUES (@p1, @p10)", ("p1", 1), ("p10", 10));

        sql.Should().Be("VALUES (1, 10)");
    }

    [Fact]
    public void NullIsNull_QuotesAreEscaped()
    {
        Build("SET X = @p0", ("p0", null)).Should().Be("SET X = NULL");
        Build("SET X = @p0", ("p0", "O'Brien")).Should().Be("SET X = 'O''Brien'");
    }
}
