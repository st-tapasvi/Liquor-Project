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
        mode.IsDetail.Should().BeFalse();
        mode.Application.MinimumLevel.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void Detail_LowersLevel_AndBackAgain()
    {
        var mode = new LogModeSwitch();

        mode.Apply(LogMode.Detail).Should().BeTrue();
        mode.IsDetail.Should().BeTrue();
        mode.Application.MinimumLevel.Should().Be(LogEventLevel.Debug);

        mode.Apply(LogMode.Normal).Should().BeTrue();
        mode.IsDetail.Should().BeFalse();
        mode.Application.MinimumLevel.Should().Be(LogEventLevel.Information);
    }

    [Fact]
    public void SameMode_ReportsNoChange() => new LogModeSwitch().Apply(LogMode.Normal).Should().BeFalse();
}
