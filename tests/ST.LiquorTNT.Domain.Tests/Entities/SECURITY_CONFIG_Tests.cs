using System.Reflection;
using FluentAssertions;
using ST.LiquorTNT.Domain.Entities;
using Xunit;

namespace ST.LiquorTNT.Domain.Tests.Entities;

public sealed class SECURITY_CONFIG_Tests
{
    private static readonly DateTime Now = new(2026, 9, 28, 10, 0, 0);

    /// <summary>SECURITY_CONFIG rows are seeded by SQL, never created in code, so tests build one by reflection.</summary>
    private static SECURITY_CONFIG Row(string value = "3")
    {
        var row = (SECURITY_CONFIG)Activator.CreateInstance(typeof(SECURITY_CONFIG), nonPublic: true)!;
        Set(row, nameof(SECURITY_CONFIG.ConfigKey), "MAX_FAILED_LOGIN_ATTEMPTS");
        Set(row, nameof(SECURITY_CONFIG.ConfigValue), value);
        Set(row, nameof(SECURITY_CONFIG.DataType), "INT");
        return row;
    }

    private static void Set(object target, string property, object value) =>
        target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance)!.SetValue(target, value);

    [Fact]
    public void UpdateValue_TrimsAndStampsAudit()
    {
        var row = Row();

        row.UpdateValue(" 5 ", updatedBy: 9, Now);

        row.ConfigValue.Should().Be("5");
        row.UpdatedBy.Should().Be(9);
        row.UpdatedAt.Should().Be(Now);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateValue_Blank_Throws(string value)
    {
        var row = Row();

        row.Invoking(r => r.UpdateValue(value, 1, Now)).Should().Throw<ArgumentException>();
        row.ConfigValue.Should().Be("3");
    }
}
