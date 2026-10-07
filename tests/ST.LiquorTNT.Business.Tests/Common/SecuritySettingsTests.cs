using FluentAssertions;
using ST.LiquorTNT.Business.Common;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Common;

public sealed class SecuritySettingsTests
{
    private static SecuritySettings From(params (string Key, string Value)[] entries) =>
        SecuritySettings.FromEntries(entries.ToDictionary(e => e.Key, e => e.Value));

    [Fact]
    public void FromEntries_WhenValuesPresent_ReadsThem()
    {
        var settings = From(
            (SecuritySettings.Keys.FailedLoginLockEnabled, "1"),
            (SecuritySettings.Keys.MaxFailedLoginAttempts, "5"),
            (SecuritySettings.Keys.AccountLockDurationMinutes, "30"),
            (SecuritySettings.Keys.SessionLimitEnabled, "0"),
            (SecuritySettings.Keys.MaxActiveSessions, "4"),
            (SecuritySettings.Keys.SessionExpiryMinutes, "60"),
            (SecuritySettings.Keys.SessionFullBehaviour, "REJECT"),
            (SecuritySettings.Keys.SecurityQuestionEnabled, "1"),
            (SecuritySettings.Keys.SecurityQuestionRequired, "2"),
            (SecuritySettings.Keys.PasswordResetExpiryMinutes, "10"),
            (SecuritySettings.Keys.PasswordResetMaxAttempts, "3"));

        settings.FailedLoginLockEnabled.Should().BeTrue();
        settings.MaxFailedLoginAttempts.Should().Be(5);
        settings.AccountLockDurationMinutes.Should().Be(30);
        settings.SessionLimitEnabled.Should().BeFalse();
        settings.MaxActiveSessions.Should().Be(4);
        settings.SessionExpiryMinutes.Should().Be(60);
        settings.SessionFullBehaviour.Should().Be("REJECT");
        settings.SecurityQuestionEnabled.Should().BeTrue();
        settings.SecurityQuestionRequired.Should().Be(2);
        settings.PasswordResetExpiryMinutes.Should().Be(10);
        settings.PasswordResetMaxAttempts.Should().Be(3);
    }

    [Fact]
    public void FromEntries_WhenKeysMissing_UsesSafeDefaults()
    {
        var settings = SecuritySettings.FromEntries(new Dictionary<string, string>());

        settings.FailedLoginLockEnabled.Should().BeTrue();
        settings.MaxFailedLoginAttempts.Should().Be(3);
        settings.AccountLockDurationMinutes.Should().Be(1440);
        settings.SessionLimitEnabled.Should().BeTrue();
        settings.MaxActiveSessions.Should().Be(2);
        settings.SessionExpiryMinutes.Should().Be(1440);
        settings.SessionFullBehaviour.Should().Be("REJECT");
        settings.PasswordResetMaxAttempts.Should().Be(5);
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("yes", true)]
    [InlineData("false", false)]
    [InlineData("no", false)]
    [InlineData("", true)]          // blank -> default (true)
    [InlineData("garbage", true)]   // unrecognised text never flips a security switch -> default
    public void FromEntries_ParsesBoolFlags_UnrecognisedFallsBack(string raw, bool expected)
    {
        From((SecuritySettings.Keys.FailedLoginLockEnabled, raw)).FailedLoginLockEnabled.Should().Be(expected);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("0")]      // zero attempts would lock on the first wrong password
    [InlineData("-1")]
    public void FromEntries_NonPositiveOrGarbageAttempts_FallBackToDefault(string raw)
    {
        From((SecuritySettings.Keys.MaxFailedLoginAttempts, raw)).MaxFailedLoginAttempts.Should().Be(3);
    }

    [Theory]
    [InlineData("0")]      // zero minutes would mean "locked until now" = never locked
    [InlineData("-5")]
    public void FromEntries_NonPositiveLockMinutes_FallBackToDefault(string raw)
    {
        From((SecuritySettings.Keys.AccountLockDurationMinutes, raw)).AccountLockDurationMinutes.Should().Be(1440);
    }

    [Fact]
    public void FromEntries_TrimsWhitespace()
    {
        var settings = From(
            (SecuritySettings.Keys.MaxActiveSessions, " 3 "),
            (SecuritySettings.Keys.SessionFullBehaviour, " REJECT "));

        settings.MaxActiveSessions.Should().Be(3);
        settings.SessionFullBehaviour.Should().Be("REJECT");
    }

    [Theory]
    [InlineData("DETAIL", true)]
    [InlineData(" detail ", true)]
    [InlineData("NORMAL", false)]
    [InlineData("VERBOSE", false)]   // unknown value never turns detail logging on
    [InlineData("", false)]
    public void FromEntries_LogMode(string raw, bool detail)
    {
        From((SecuritySettings.Keys.LogMode, raw)).DetailLogging.Should().Be(detail);
    }

    [Fact]
    public void FromEntries_NoLogMode_IsNormal() => From().DetailLogging.Should().BeFalse();
}
