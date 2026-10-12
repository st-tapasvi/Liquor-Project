using FluentAssertions;
using ST.LiquorTNT.Domain.Rules;
using Xunit;

namespace ST.LiquorTNT.Domain.Tests.Rules;

/// <summary>Which excises a page / action exists in: null = every excise; an action needs its page AND itself to cover the excise.</summary>
public sealed class ExciseScopeTests
{
    [Fact]
    public void Null_CoversEveryExcise() => ExciseScope.Covers(null, "JK").Should().BeTrue();

    [Theory]
    [InlineData("RJ", true)]
    [InlineData("rj", true)]       // codes compare without case
    [InlineData("JK", false)]
    public void List_CoversOnlyItsExcises(string exciseCode, bool expected) =>
        ExciseScope.Covers(new[] { "RJ", "UP" }, exciseCode).Should().Be(expected);

    [Fact]
    public void Action_NeedsPageAndActionToCover()
    {
        ExciseScope.ActionCovers(new[] { "RJ", "UP" }, null, "UP").Should().BeTrue();          // action follows its page
        ExciseScope.ActionCovers(new[] { "RJ", "UP" }, new[] { "RJ" }, "UP").Should().BeFalse(); // action narrower than its page
        ExciseScope.ActionCovers(null, new[] { "RJ" }, "RJ").Should().BeTrue();                // common page, RJ-only action
        ExciseScope.ActionCovers(new[] { "JK" }, new[] { "RJ" }, "RJ").Should().BeFalse();       // page not in RJ at all
    }

    [Fact]
    public void ActionCoversAny_ForACompanyLevelRole_NullMeansNoRestriction()
    {
        ExciseScope.ActionCoversAny(new[] { "JK" }, null, new[] { "RJ", "JK" }).Should().BeTrue();
        ExciseScope.ActionCoversAny(new[] { "JK" }, null, new[] { "RJ" }).Should().BeFalse();
        ExciseScope.ActionCoversAny(new[] { "JK" }, null, null).Should().BeTrue();
    }
}
