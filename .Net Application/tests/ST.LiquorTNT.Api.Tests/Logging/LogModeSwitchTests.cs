using FluentAssertions;
using Serilog.Events;
using ST.LiquorTNT.Logging;
using Xunit;

namespace ST.LiquorTNT.Api.Tests.Logging;

public sealed class LogModeSwitchTests
{
    [Fact]
    public void StartsNormal()
    {
        var mode = new LogModeSwitch();

        mode.Mode.Should().Be(LogMode.Normal);
        mode.Application.MinimumLevel.Should().Be(LogEventLevel.Information);
        mode.Sql.MinimumLevel.Should().Be(LogEventLevel.Warning);        // no SQL in NORMAL
    }

    [Fact]
    public void Detail_ShowsSql_AndBackAgain()
    {
        var mode = new LogModeSwitch();

        mode.Apply(LogMode.Detail).Should().BeTrue();
        mode.Application.MinimumLevel.Should().Be(LogEventLevel.Debug);
        mode.Sql.MinimumLevel.Should().Be(LogEventLevel.Information);   // EF "Executed DbCommand"

        mode.Apply(LogMode.Normal).Should().BeTrue();
        mode.Sql.MinimumLevel.Should().Be(LogEventLevel.Warning);
    }

    [Fact]
    public void SameMode_ReportsNoChange() => new LogModeSwitch().Apply(LogMode.Normal).Should().BeFalse();
}
