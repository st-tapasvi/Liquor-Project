using System.Text.Json;
using FluentAssertions;
using Serilog.Events;
using Serilog.Parsing;
using ST.LiquorTNT.Logging;
using Xunit;

namespace ST.LiquorTNT.Api.Tests.Logging;

/// <summary>Every entry has the same keys, in the same order, and reads like a Postman response.</summary>
public sealed class AppJsonFormatterTests
{
    private static (string Text, JsonElement Json) Format(LogEvent logEvent)
    {
        var output = new StringWriter();
        new AppJsonFormatter().Format(logEvent, output);

        var text = output.ToString();
        return (text, JsonDocument.Parse(text).RootElement.Clone());
    }

    private static LogEvent Event(string template, Exception? ex = null, LogEventLevel level = LogEventLevel.Warning, params (string Name, object? Value)[] properties) =>
        new(new DateTimeOffset(2026, 9, 28, 11, 11, 32, 339, TimeSpan.Zero), level, ex,
            new MessageTemplateParser().Parse(template),
            properties.Select(p => new LogEventProperty(p.Name, new ScalarValue(p.Value))));

    [Fact]
    public void FixedKeysInOrder_IstTime_Indented()
    {
        var (text, json) = Format(Event("{HttpMethod} {Path} -> {StatusCode}", null, LogEventLevel.Warning,
            ("HttpMethod", "POST"), ("Path", "/api/auth/login"), ("StatusCode", 401),
            ("Method", "AuthController.LoginAsync"), ("CorrelationId", "abc123")));

        text.Should().StartWith("{" + Environment.NewLine + "  \"Timestamp\"");                // one field per line
        json.EnumerateObject().Select(p => p.Name).Should().Equal("Timestamp", "Level", "CorrelationId", "Method", "Message", "Context");
        json.GetProperty("Timestamp").GetString().Should().Be("2026-09-28 16:41:32.339");    // 11:11 UTC = 16:41 IST
        json.GetProperty("Level").GetString().Should().Be("Warning");
        json.GetProperty("CorrelationId").GetString().Should().Be("abc123");
        json.GetProperty("Method").GetString().Should().Be("AuthController.LoginAsync");
        json.GetProperty("Message").GetString().Should().Be("POST /api/auth/login -> 401");
        json.GetProperty("Context").ValueKind.Should().Be(JsonValueKind.Null);              // everything was in the message
    }

    [Theory]
    [InlineData(LogEventLevel.Information, "Info")]
    [InlineData(LogEventLevel.Error, "Error")]
    [InlineData(LogEventLevel.Fatal, "Fatal")]
    public void LevelNames(LogEventLevel level, string name) =>
        Format(Event("x", null, level)).Json.GetProperty("Level").GetString().Should().Be(name);

    [Fact]
    public void Context_HoldsTheRest_PlumbingAndEmptyValuesLeftOut()
    {
        var (_, json) = Format(Event("call", null, LogEventLevel.Information,
            ("RequestId", "0HN:1"), ("ConnectionId", "0HN"), ("UserId", null), ("Query", ""), ("IpAddress", "::1")));

        json.GetProperty("Context").EnumerateObject().Select(p => p.Name).Should().Equal("ipAddress");
    }

    [Fact]
    public void BodiesInputsOutputs_AreRealJsonObjects_NotEscapedStrings()
    {
        var (_, json) = Format(Event("call", null, LogEventLevel.Information,
            ("Input", "{\"userName\":\"ravi\",\"password\":\"***\"}"), ("Output", "5"), ("Response", "<body not logged: x>")));

        var context = json.GetProperty("Context");
        context.GetProperty("input").GetProperty("userName").GetString().Should().Be("ravi");
        context.GetProperty("output").GetInt32().Should().Be(5);
        context.GetProperty("response").GetString().Should().Be("<body not logged: x>");   // not JSON -> plain text
    }

    [Theory]
    [InlineData("Microsoft.Hosting.Lifetime", "Startup")]
    [InlineData("ST.LiquorTNT.Api.Logging.LogModeRefresher", "LogModeRefresher")]
    public void Method_FallsBackToTheLoggingClass(string source, string method)
    {
        var (_, json) = Format(Event("x", null, LogEventLevel.Information, ("SourceContext", source)));

        json.GetProperty("Method").GetString().Should().Be(method);
    }

    [Fact]
    public void Sql_ShortMessage_QueryAsLines()
    {
        var (_, json) = Format(Event("Executed DbCommand ({elapsed}ms) [Parameters=[{parameters}]]{newLine}{commandText}", null, LogEventLevel.Information,
            ("SourceContext", LogModeSwitch.SqlCategory), ("elapsed", "2"), ("parameters", "@p0='?'"), ("newLine", "\r\n"),
            ("commandText", "SELECT `u`.`ID`\r\nFROM `USERS` AS `u`")));

        json.GetProperty("Method").GetString().Should().Be("SQL");
        json.GetProperty("Message").GetString().Should().Be("SQL executed in 2 ms");
        json.GetProperty("Context").GetProperty("sql").EnumerateArray().Select(l => l.GetString())
            .Should().Equal("SELECT `u`.`ID`", "FROM `USERS` AS `u`");
    }

    [Fact]
    public void Exception_TypeMessageInnerCauseAndStack()
    {
        Exception thrown;
        try
        {
            try { throw new TimeoutException("db timeout"); }
            catch (Exception inner) { throw new InvalidOperationException("boom", inner); }
        }
        catch (Exception ex) { thrown = ex; }

        var (_, json) = Format(Event("failed", thrown, LogEventLevel.Error));

        var exception = json.GetProperty("Exception");
        exception.GetProperty("ExceptionType").GetString().Should().Be("System.InvalidOperationException");
        exception.GetProperty("ExceptionMessage").GetString().Should().Be("boom");
        exception.GetProperty("InnerExceptionType").GetString().Should().Be("System.TimeoutException");
        exception.GetProperty("InnerException").GetString().Should().Be("db timeout");
        exception.GetProperty("StackTrace").EnumerateArray().Should().Contain(l => l.GetString()!.Contains(nameof(Exception_TypeMessageInnerCauseAndStack)));
    }

    [Fact]
    public void NoException_NoExceptionKey() =>
        Format(Event("fine")).Json.TryGetProperty("Exception", out _).Should().BeFalse();

    [Fact]
    public void QuotesAndNewLines_StayValidJson()
    {
        var (_, json) = Format(Event("{Text}", null, LogEventLevel.Information, ("Text", "say \"hi\"\r\nnext")));

        json.GetProperty("Message").GetString().Should().Be("say \"hi\"\r\nnext");
    }
}
