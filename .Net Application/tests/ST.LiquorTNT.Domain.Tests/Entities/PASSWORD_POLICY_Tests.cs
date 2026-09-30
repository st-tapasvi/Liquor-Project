using FluentAssertions;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Domain.Tests.Entities;

public sealed class PASSWORD_POLICY_Tests
{
    private static readonly DateTime Now = new(2026, 9, 28, 10, 0, 0);

    private static PASSWORD_POLICY Create(int min = 8, int max = 64, bool expiryEnabled = false, int? expiryDays = null) =>
        PASSWORD_POLICY.Create("P", min, max, true, true, true, false, 5, expiryEnabled, expiryDays, false, false, Now, 1);

    [Fact]
    public void Create_StoresRules_ActiveByDefault()
    {
        var policy = Create(min: 12, max: 20, expiryEnabled: true, expiryDays: 90);

        policy.PolicyName.Should().Be("P");
        policy.MinLength.Should().Be(12);
        policy.MaxLength.Should().Be(20);
        policy.PasswordExpiryDays.Should().Be(90);
        policy.Status.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 10)]     // min below 1
    [InlineData(10, 9)]     // max below min
    public void UpdateRules_InvalidLengths_Throws(int min, int max)
    {
        var act = () => Create(min, max);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void UpdateRules_ExpiryEnabledWithoutValidDays_Throws(int? days)
    {
        var act = () => Create(expiryEnabled: true, expiryDays: days);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateRules_NegativeHistory_Throws()
    {
        var act = () => PASSWORD_POLICY.Create("P", 8, 64, false, false, false, false, -1, false, null, false, false, Now, 1);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateRules_DisablingExpiry_ClearsDays()
    {
        var policy = Create(expiryEnabled: true, expiryDays: 30);

        policy.UpdateRules(8, 64, false, false, false, false, 5, passwordExpiryEnabled: false, 30, false, false, Now, 2);

        policy.PasswordExpiryEnabled.Should().BeFalse();
        policy.PasswordExpiryDays.Should().BeNull();
        policy.UpdatedBy.Should().Be(2);
    }

    [Fact]
    public void ExpiryFrom_EnabledAddsDays_DisabledIsNull()
    {
        Create(expiryEnabled: true, expiryDays: 90).ExpiryFrom(Now).Should().Be(Now.AddDays(90));
        Create(expiryEnabled: false).ExpiryFrom(Now).Should().BeNull();
    }
}
