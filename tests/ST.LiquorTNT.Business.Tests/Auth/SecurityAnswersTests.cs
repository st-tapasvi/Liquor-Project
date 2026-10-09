using FluentAssertions;
using ST.LiquorTNT.Business.Auth;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Auth;

/// <summary>How a security answer is compared: case and every space are ignored; spelling and punctuation count.</summary>
public sealed class SecurityAnswersTests
{
    [Theory]
    [InlineData("New Delhi", "newdelhi")]
    [InlineData("New Delhi", "  NEW   DELHI ")]
    [InlineData("Jaipur", "JAIPUR")]
    [InlineData("Ram Nagar", "ram\tnagar")]
    public void Same_WhenOnlyCaseOrSpacesDiffer(string saved, string typed) =>
        SecurityAnswers.Normalize(typed).Should().Be(SecurityAnswers.Normalize(saved));

    [Theory]
    [InlineData("Jaipur", "Jaypur")]            // spelling
    [InlineData("St. Mary's", "St Marys")]      // punctuation
    [InlineData("Ram-Nagar", "Ram Nagar")]      // hyphen is not a space
    public void Different_WhenSpellingOrPunctuationDiffers(string saved, string typed) =>
        SecurityAnswers.Normalize(typed).Should().NotBe(SecurityAnswers.Normalize(saved));
}
