using FluentAssertions;
using ST.LiquorTNT.Business.Tests.Fakes;
using ST.LiquorTNT.Business.Users;
using Xunit;

namespace ST.LiquorTNT.Business.Tests.Users;

public sealed class PasswordPolicyValidatorTests
{
    private readonly PasswordPolicyValidator _validator = new(new FakePasswordHasher());
    private static readonly string[] NoHistory = Array.Empty<string>();

    private IReadOnlyList<string> Validate(string password, Domain.Entities.PASSWORD_POLICY policy, string? userName = "alice", string[]? history = null)
        => _validator.Validate(password, userName, policy, history ?? NoHistory);

    // ---------- length boundaries ----------

    [Theory]
    [InlineData(8, true)]    // exactly min -> ok
    [InlineData(7, false)]   // one below min -> error
    public void MinLength_IsInclusive(int length, bool ok)
    {
        var errors = Validate(new string('a', length), TestData.Policy(minLength: 8, maxLength: 64));
        (errors.Count == 0).Should().Be(ok);
    }

    [Theory]
    [InlineData(20, true)]   // exactly max -> ok
    [InlineData(21, false)]  // one above max -> error
    public void MaxLength_IsInclusive(int length, bool ok)
    {
        var errors = Validate(new string('a', length), TestData.Policy(minLength: 1, maxLength: 20));
        (errors.Count == 0).Should().Be(ok);
    }

    // ---------- character class rules ----------

    [Fact]
    public void RequireUppercase_MissingUpper_Reports()
    {
        Validate("abcdefgh1", TestData.Policy(upper: true)).Should().ContainSingle(e => e.Contains("uppercase"));
        Validate("Abcdefgh1", TestData.Policy(upper: true)).Should().BeEmpty();
    }

    [Fact]
    public void RequireLowercase_MissingLower_Reports()
    {
        Validate("ABCDEFGH1", TestData.Policy(lower: true)).Should().ContainSingle(e => e.Contains("lowercase"));
    }

    [Fact]
    public void RequireNumber_MissingDigit_Reports()
    {
        Validate("abcdefghij", TestData.Policy(number: true)).Should().ContainSingle(e => e.Contains("number"));
    }

    [Fact]
    public void RequireSpecial_MissingSpecial_Reports()
    {
        Validate("abcdefgh1", TestData.Policy(special: true)).Should().ContainSingle(e => e.Contains("special"));
        Validate("abcdefgh1!", TestData.Policy(special: true)).Should().BeEmpty();
    }

    [Fact]
    public void RulesOff_NoCharacterClassErrors()
    {
        Validate("abcdefghij", TestData.Policy()).Should().BeEmpty();
    }

    // ---------- user name ----------

    [Theory]
    [InlineData("xxalicexx")]
    [InlineData("xxALICExx")]      // case-insensitive
    public void UserNameInPassword_NotAllowed_Reports(string password)
    {
        Validate(password, TestData.Policy(allowUserName: false), userName: "alice")
            .Should().ContainSingle(e => e.Contains("user name"));
    }

    [Fact]
    public void UserNameInPassword_Allowed_NoError()
    {
        Validate("xxalicexx", TestData.Policy(allowUserName: true), userName: "alice").Should().BeEmpty();
    }

    [Fact]
    public void UserNameNull_NoUserNameCheck()
    {
        Validate("whatever1", TestData.Policy(), userName: null).Should().BeEmpty();
    }

    // ---------- common passwords ----------

    [Fact]
    public void CommonPassword_NotAllowed_Reports_CaseInsensitive()
    {
        Validate("Password123", TestData.Policy(allowCommon: false)).Should().ContainSingle(e => e.Contains("too common"));
        Validate("Password123", TestData.Policy(allowCommon: true)).Should().BeEmpty();
    }

    // ---------- history ----------

    [Fact]
    public void ReusedRecentPassword_Reports()
    {
        var history = new[] { "H:OldPass1", "H:OldPass2" };

        Validate("OldPass2", TestData.Policy(historyCount: 5), history: history)
            .Should().ContainSingle(e => e.Contains("last 5"));
        Validate("NewPass3", TestData.Policy(historyCount: 5), history: history).Should().BeEmpty();
    }

    // ---------- aggregation ----------

    [Fact]
    public void ManyViolations_AllReported()
    {
        var policy = TestData.Policy(minLength: 12, upper: true, lower: true, number: true, special: true);

        var errors = Validate("abc", policy);

        errors.Should().HaveCount(4);   // too short, no upper, no number, no special (has lower)
    }
}
